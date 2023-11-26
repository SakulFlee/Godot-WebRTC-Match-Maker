use std::error::Error;

use serde::{Deserialize, Serialize};

use crate::{MatchMakingRequest, PacketType};

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct Packet {
    #[serde(rename = "type")]
    pub ty: PacketType,
    pub from: String,
    pub to: String,
    pub json: String,
}

impl Packet {
    pub fn from_json(json: &str) -> Result<Self, serde_json::Error> {
        serde_json::from_str::<Packet>(json)
    }

    pub fn to_json(&self) -> Result<String, serde_json::Error> {
        serde_json::to_string(&self)
    }

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
