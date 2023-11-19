use serde::{Deserialize, Serialize};

use crate::{ICECandidateRequest, MatchMakingRequest, SessionDescriptionRequest};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Request {
    MatchMaking(MatchMakingRequest),
    SessionDescription(SessionDescriptionRequest),
    ICECandidate(ICECandidateRequest),
}
