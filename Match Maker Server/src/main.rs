use std::{
    collections::HashMap,
    error::Error,
    str::FromStr,
    sync::{Arc, Mutex},
};

use match_maker_server::{
    AppConfig, MatchMakingQuery, MatchMakingRequest, MatchMakingResponse, Packet, PacketType,
};
use uuid::Uuid;
use ws::Sender;

pub struct Handler {
    local_sender: Sender,
    local_uuid: Uuid,
    app_config: Arc<AppConfig>,
    queue: Arc<Mutex<HashMap<String, MatchMakingQuery>>>,
    peers: Arc<Mutex<HashMap<Uuid, Sender>>>,
}

impl Handler {
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
            match lock.get_mut(&request.name) {
                Some(query) => {
                    // Query exists -> Add peer
                    query.add_peer(self.local_uuid);

                    // Check if room is full
                    if query.is_filled(slot_requirement) {
                        if let Ok(lock) = self.peers.lock() {
                            let host_uuid = query.peers[0].to_string();
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

                    return Ok(());
                }
                None => {
                    // Query doesn't exist -> Create
                    let mut query: MatchMakingQuery = request.clone().into();
                    query.add_peer(self.local_uuid);
                    lock.insert(query.name.clone(), query);

                    return Ok(());
                }
            }
        }

        Err(ws::Error::new(
            ws::ErrorKind::Protocol,
            format!("Unknown error ocurred (race condition?)"),
        ))
    }
}

impl ws::Handler for Handler {
    fn on_open(&mut self, _shake: ws::Handshake) -> ws::Result<()> {
        // Add to peers
        if let Ok(mut lock) = self.peers.lock() {
            lock.insert(self.local_uuid, self.local_sender.clone());
        }

        Ok(())
    }

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

    fn on_message(&mut self, msg: ws::Message) -> ws::Result<()> {
        let packet = Packet::from_json(&msg.as_text()?).map_err(|e| {
            ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
        })?;
        println!("Packet: {:?}", packet);

        if packet.ty == PacketType::MatchMakerRequest {
            // Match Maker request!

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
                format!("Why would you send a MatchMakerRESPONSE to the server ... ?"),
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
                            format!("Invalid UUID!"),
                        ))
                    }
                }
            }
        }

        // let request: Request = serde_json::from_str(&msg.as_text()?).map_err(|e| {
        //     ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
        // })?;
        // println!("Request: {:?}", request);

        // match request {
        //     Request::MatchMaking(request) => self.handle_match_making(request)?,
        //     Request::SessionDescription(request) => self.handle_session_description(request)?,
        //     Request::ICECandidate(request) => self.handle_ice_candidate(request)?,
        // }

        Ok(())
    }
}

fn main() -> Result<(), Box<dyn Error>> {
    let app_config = Arc::new(AppConfig::load()?);
    let queue = Arc::new(Mutex::new(HashMap::new()));
    let peers = Arc::new(Mutex::new(HashMap::new()));

    println!("Match Maker Server listening on 0.0.0.0:33333 ...");
    ws::listen(app_config.listen_string(), |sender| Handler {
        local_sender: sender,
        local_uuid: Uuid::new_v4(),
        app_config: app_config.clone(),
        queue: queue.clone(),
        peers: peers.clone(),
    })?;

    Ok(())
}
