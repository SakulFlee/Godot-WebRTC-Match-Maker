use uuid::Uuid;

use crate::MatchMakingRequest;

#[derive(Debug, Clone)]
pub struct MatchMakingQuery {
    pub name: String,
    pub peers: Vec<Uuid>,
}

impl MatchMakingQuery {
    pub fn add_peer(&mut self, uuid: Uuid) {
        self.peers.push(uuid);
    }

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

    pub fn is_filled(&self, requirement: usize) -> bool {
        self.peers.len() >= requirement
    }
}

impl From<MatchMakingRequest> for MatchMakingQuery {
    fn from(value: MatchMakingRequest) -> Self {
        Self {
            name: value.name,
            peers: Vec::new(),
        }
    }
}
