use std::error::Error;

use serde::{Deserialize, Serialize};

use crate::MatchMakingRequest;

/// Packet Types
mod packet_type;
pub use packet_type::*;

/// Incoming or outgoing packet received or send by the server
#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct Packet {
    #[serde(rename = "type")]
    pub ty: PacketType,
    pub from: String,
    pub to: String,
    pub json: String,
}

impl Packet {
    /// Turns a JSON string into a Packet
    pub fn from_json(json: &str) -> Result<Self, serde_json::Error> {
        serde_json::from_str::<Packet>(json)
    }

    /// Turns a packet into a JSON string
    pub fn to_json(&self) -> Result<String, serde_json::Error> {
        serde_json::to_string(&self)
    }

    /// Parses the internally nested [`MatchMakingRequest`].
    ///
    /// **Only** works, if the internally nested JSON
    /// actually IS a [`MatchMakingRequest`].
    pub fn parse_match_making_request(&self) -> Result<MatchMakingRequest, Box<dyn Error>> {
        if self.ty != PacketType::MatchMakerRequest {
            return Err(format!(
                "Trying to parse MatchMakingRequest with invalid type ({:?})",
                &self.ty
            )
            .into());
        }

        Ok(serde_json::from_str::<MatchMakingRequest>(&self.json)?)
    }
}
