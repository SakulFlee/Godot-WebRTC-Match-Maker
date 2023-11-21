use std::{
    collections::HashMap,
    error::Error,
    str::FromStr,
    sync::{Arc, Mutex},
};

use uuid::Uuid;
use ws::Sender;

mod app_config;
pub use app_config::*;

mod request;
pub use request::*;

mod response;
pub use response::*;

mod match_making_request;
pub use match_making_request::*;

mod match_making_response;
pub use match_making_response::*;

mod match_making_query;
pub use match_making_query::*;

mod session_description_request;
pub use session_description_request::*;

mod ice_candidate_request;
pub use ice_candidate_request::*;

mod session_description_response;
pub use session_description_response::*;

mod ice_candidate_response;
pub use ice_candidate_response::*;

mod peer;
pub use peer::*;

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
                            // First peer is host, others are clients
                            let host = &query.peers[0];
                            let clients = &query.peers[1..query.peers.len()].to_vec();

                            let host_peer = &lock[host];

                            let match_making_response = MatchMakingResponse {
                                is_host: true,
                                peers: clients.iter().map(|x| x.to_string()).collect(),
                            };

                            let response = Response::MatchMaking(match_making_response);
                            let response_json = serde_json::to_string(&response).map_err(|e| {
                                ws::Error::new(
                                    ws::ErrorKind::Protocol,
                                    format!("Invalid request: {}", e),
                                )
                            })?;

                            host_peer.send(response_json)?;

                            // Others are clients
                            for client in clients {
                                let client_peer = &lock[client];

                                let match_making_response = MatchMakingResponse {
                                    is_host: false,
                                    peers: vec![host.to_string()],
                                };
                                let response = Response::MatchMaking(match_making_response);

                                let response_json =
                                    serde_json::to_string(&response).map_err(|e| {
                                        ws::Error::new(
                                            ws::ErrorKind::Protocol,
                                            format!("Invalid request: {}", e),
                                        )
                                    })?;

                                client_peer.send(response_json)?;
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

    fn handle_session_description(&mut self, request: SessionDescriptionRequest) -> ws::Result<()> {
        let uuid = Uuid::from_str(&request.uuid).map_err(|e| {
            ws::Error::new(
                ws::ErrorKind::Protocol,
                format!("Invalid request: UUID is invalid! ({})", e),
            )
        })?;

        if let Ok(lock) = self.peers.lock() {
            match lock.get(&uuid) {
                Some(peer) => {
                    // Request seems to be valid -> send the packet
                    let mut session_description_response: SessionDescriptionResponse =
                        request.into();
                    session_description_response.uuid = self.local_uuid.to_string();

                    let response = Response::SessionDescription(session_description_response);
                    let response_json = serde_json::to_string(&response).map_err(|e| {
                        ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
                    })?;

                    peer.send(response_json)?;

                    return Ok(());
                }
                None => {
                    // Peer UUID can't be found -> Request is invalid
                    return Err(ws::Error::new(
                        ws::ErrorKind::Protocol,
                        format!("Invalid request: UUID is invalid!"),
                    ));
                }
            }
        }

        Err(ws::Error::new(
            ws::ErrorKind::Protocol,
            format!("Unknown error ocurred (race condition?)"),
        ))
    }

    fn handle_ice_candidate(&mut self, request: ICECandidateRequest) -> ws::Result<()> {
        let uuid = Uuid::from_str(&request.uuid).map_err(|e| {
            ws::Error::new(
                ws::ErrorKind::Protocol,
                format!("Invalid request: UUID is invalid! ({})", e),
            )
        })?;

        if let Ok(lock) = self.peers.lock() {
            match lock.get(&uuid) {
                Some(peer) => {
                    // Request seems to be valid -> send the packet
                    let mut ice_candidate_response: ICECandidateResponse = request.into();
                    ice_candidate_response.uuid = self.local_uuid.to_string();

                    let response = Response::ICECandidate(ice_candidate_response);
                    let response_json = serde_json::to_string(&response).map_err(|e| {
                        ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
                    })?;

                    peer.send(response_json)?;

                    return Ok(());
                }
                None => {
                    // Peer UUID can't be found -> Request is invalid
                    return Err(ws::Error::new(
                        ws::ErrorKind::Protocol,
                        format!("Invalid request: UUID is invalid!"),
                    ));
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
        let request: Request = serde_json::from_str(&msg.as_text()?).map_err(|e| {
            ws::Error::new(ws::ErrorKind::Protocol, format!("Invalid request: {}", e))
        })?;
        println!("Request: {:?}", request);

        match request {
            Request::MatchMaking(request) => self.handle_match_making(request)?,
            Request::SessionDescription(request) => self.handle_session_description(request)?,
            Request::ICECandidate(request) => self.handle_ice_candidate(request)?,
        }

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
