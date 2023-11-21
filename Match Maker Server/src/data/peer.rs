use ws::Sender;

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Peer {
    pub sender: Sender,
}
