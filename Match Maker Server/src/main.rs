#![allow(clippy::result_large_err)] // WS-RS issue

use std::{
    collections::HashMap,
    error::Error,
    str::FromStr,
    sync::{Arc, Mutex},
};

use match_maker_server::{
    AppConfig, MatchMakingQueue, MatchMakingRequest, MatchMakingResponse, Packet, PacketType,
};
use simple_logger::SimpleLogger;
use uuid::Uuid;
use ws::Sender;

/// Main Handler for WS-RS
pub struct Handler {
    /// The local handle for the connection
    local_sender: Sender,
    /// The local assigned UUID for this connection
    local_uuid: Uuid,
    /// App configuration, mostly for queue filling
    app_config: Arc<AppConfig>,
    /// Queue lookup-table
    queue: Arc<Mutex<HashMap<String, MatchMakingQueue>>>,
    /// Peer lookup-table
    peers: Arc<Mutex<HashMap<Uuid, Sender>>>,
}

impl Handler {
    /// Handles Match Making requests.
    /// Main server logic is in here.
    ///
    /// On request, we do:
    /// 1. Validate the request
    /// 2. Check if a queue exists for the request
    /// 3a. If there is no queue: Add a new queue
    /// 3b. If there is a queue: Add the peer to the queue
    /// 4. Check if the queue is filled
    /// 5. If filled: Send a response packet back to each peer,
    /// containing all information needed to connect
    /// to each other.
    fn handle_match_making(&mut self, request: MatchMakingRequest) -> ws::Result<()> {
        let slot_requirement = match self.app_config.slots.get(&request.name) {
            Some(slot) => *slot as usize,
            None => {
                return Err(ws::Error::new(
                    ws::ErrorKind::Protocol,
                    String::from(
                        "Invalid request: Slot config does not exist for the requested queue!",
                    ),
                ))
            }
        };

        if let Ok(mut lock) = self.queue.lock() {
            let mut remove = false;
            match lock.get_mut(&request.name) {
                Some(query) => {
                    // Query exists -> Add peer
                    query.add_peer(self.local_uuid);

                    // Check if room is full
                    if query.is_filled(slot_requirement) {
                        remove = true;

                        if let Ok(lock) = self.peers.lock() {
                            let host_uuid = query.peers[0].to_string();

                            // For each peer, send a Response packet
                            for peer in &query.peers {
                                let _ = &lock[peer].send(
                                    Packet {
                                        ty: PacketType::MatchMakerResponse,
                                        from: String::from("MatchMaker"),
                                        to: peer.to_string(),
                                        json: serde_json::to_string(&MatchMakingResponse {
                                            own_uuid: peer.to_string(),
                                            host_uuid: host_uuid.clone(),
                                            peers: query
                                                .peers
                                                .iter()
                                                .map(|x| x.to_string())
                                                .collect(),
                                        })
                                        .map_err(|e| {
                                            ws::Error::new(
                                                ws::ErrorKind::Protocol,
                                                format!("Invalid request: {}", e),
                                            )
                                        })?,
                                    }
                                    .to_json()
                                    .map_err(|e| {
                                        ws::Error::new(
                                            ws::ErrorKind::Protocol,
                                            format!("Invalid request: {}", e),
                                        )
                                    })?,
                                )?;
                            }
                        }
                    }
                }
                None => {
                    // Query doesn't exist -> Create
                    let mut query: MatchMakingQueue = request.clone().into();
                    query.add_peer(self.local_uuid);
                    lock.insert(query.name.clone(), query);

                    return Ok(());
                }
            }

            if remove {
                lock.remove(&request.name);
            }

            return Ok(());
        }

        Err(ws::Error::new(
            ws::ErrorKind::Protocol,
            "Unknown error ocurred (race condition?)".to_string(),
        ))
    }
}

impl ws::Handler for Handler {
    /// A connection got made to the server.
    ///
    /// We want to add the connection to our peers list.
    fn on_open(&mut self, _shake: ws::Handshake) -> ws::Result<()> {
        // Add to peers
        if let Ok(mut lock) = self.peers.lock() {
            lock.insert(self.local_uuid, self.local_sender.clone());
        }

        Ok(())
    }

    /// A connection is closed.
    ///
    /// The peer should be removed from peers and any queues.
    fn on_close(&mut self, _code: ws::CloseCode, _reason: &str) {
        // Remove peer
        if let Ok(mut lock) = self.peers.lock() {
            lock.remove(&self.local_uuid);
        }

        // Remove from queue
        if let Ok(mut lock) = self.queue.lock() {
            lock.values_mut().for_each(|x| {
                if let Some(index) = x.peers.iter_mut().position(|x| *x == self.local_uuid) {
                    x.peers.remove(index);
                }
            });
        }
    }

    /// A message is received.
    ///
    /// Validate & Parse the package, then either send it
    /// to be handled (in case of a MatchMaker request), or,
    /// relay the packet to the peer.
    fn on_message(&mut self, msg: ws::Message) -> ws::Result<()> {
        let packet = Packet::from_json(msg.as_text()?).map_err(|e| {
            ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
        })?;
        log::debug!("Packet: {:?}", packet);

        if packet.ty == PacketType::MatchMakerRequest {
            // Match Maker request!
            // Validate and send it to the MatchMaker handler

            if packet.from != "UNKNOWN" {
                return Err(ws::Error::new(ws::ErrorKind::Protocol, format!("Initial MatchMakingRequest packet with known PeerUUID is impossible! (from: 'MatchMaker' != from: '{}')", packet.from)));
            }

            if packet.to != "MatchMaker" {
                return Err(ws::Error::new(ws::ErrorKind::Protocol, format!("Initial MatchMakingRequest packet without addressing it to MatchMaker (to: 'MatchMaker' != to: '{}')", packet.to)));
            }

            let match_maker_request: MatchMakingRequest = serde_json::from_str(&packet.json)
                .map_err(|e| {
                    ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
                })?;

            self.handle_match_making(match_maker_request)?;
        } else if packet.ty == PacketType::MatchMakerResponse {
            // Very invalid package ... Seriously why send this?

            return Err(ws::Error::new(
                ws::ErrorKind::Protocol,
                "Why would you send a MatchMakerRESPONSE to the server ... ?".to_string(),
            ));
        } else {
            // Relay mode
            // Any other package that falls in this block doesn't
            // need to be parsed! We can just relay that to the
            // peer it's intended to be.
            // UUID checking is done locally on each peer too!

            if let Ok(peers) = self.peers.lock() {
                let uuid = Uuid::from_str(&packet.to).map_err(|e| {
                    ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
                })?;

                match peers.get(&uuid) {
                    Some(peer) => {
                        return peer.send(packet.to_json().map_err(|e| {
                            ws::Error::new(
                                ws::ErrorKind::Protocol,
                                format!("Invalid request: {}", e),
                            )
                        })?)
                    }
                    None => {
                        return Err(ws::Error::new(
                            ws::ErrorKind::Protocol,
                            "Invalid UUID!".to_string(),
                        ))
                    }
                }
            }
        }

        Ok(())
    }
}

fn main() -> Result<(), Box<dyn Error>> {
    SimpleLogger::new()
        .with_level(log::LevelFilter::Info)
        .env()
        .init()
        .unwrap();
    log::info!("Logging level is set to {}!", log::max_level());

    let app_config = Arc::new(AppConfig::load()?);
    let queue = Arc::new(Mutex::new(HashMap::new()));
    let peers = Arc::new(Mutex::new(HashMap::new()));

    log::info!("Match Maker Server listening on 0.0.0.0:33333 ...");
    ws::listen(app_config.listen_string(), |sender| Handler {
        local_sender: sender,
        local_uuid: Uuid::new_v4(),
        app_config: app_config.clone(),
        queue: queue.clone(),
        peers: peers.clone(),
    })?;

    Ok(())
}
