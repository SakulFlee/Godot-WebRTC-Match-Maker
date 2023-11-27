use ws::Sender;

/// Peer data struct
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Peer {
    /// Connection socket to peer
    pub sender: Sender,
}
