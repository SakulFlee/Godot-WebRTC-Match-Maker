use serde::{Deserialize, Serialize};

use crate::{ICECandidateResponse, MatchMakingResponse, SessionDescriptionResponse};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Response {
    MatchMaking(MatchMakingResponse),
    SessionDescription(SessionDescriptionResponse),
    ICECandidate(ICECandidateResponse),
}
