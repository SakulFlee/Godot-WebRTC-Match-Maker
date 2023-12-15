use uuid::Uuid;

use super::match_making_request::MatchMakingRequest;

/// Query for Match Maker.
///
/// Holds information about the current on-going queue.
#[derive(Debug, Clone)]
pub struct MatchMakingQueue {
    /// Name of the queue
    pub name: String,
    /// List of peers
    pub peers: Vec<Uuid>,
}

impl MatchMakingQueue {
    /// Adds a peer to the queue
    pub fn add_peer(&mut self, uuid: Uuid) {
        self.peers.push(uuid);
    }

    /// Removes a peer from the queue
    pub fn remove_peer(&mut self, uuid: Uuid) {
        let to_be_removed: Vec<usize> = self
            .peers
            .iter()
            .enumerate()
            .filter(|(_, x)| **x == uuid)
            .map(|(x, _)| x)
            .collect();

        to_be_removed.iter().for_each(|x| {
            self.peers.remove(*x);
        });
    }

    /// Returns `true` if the queue is fulfilling the
    /// set requirement, false otherwise.
    pub fn is_filled(&self, requirement: usize) -> bool {
        self.peers.len() >= requirement
    }
}

impl From<MatchMakingRequest> for MatchMakingQueue {
    fn from(value: MatchMakingRequest) -> Self {
        Self {
            name: value.name,
            peers: Vec::new(),
        }
    }
}
