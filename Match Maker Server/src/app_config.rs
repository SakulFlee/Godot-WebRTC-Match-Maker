use std::{collections::HashMap, error::Error, fmt::Display};

use serde::{Deserialize, Serialize};

/// App Configuration
///
/// Defines all options for the app
#[derive(Debug, Serialize, Deserialize)]
pub struct AppConfig {
    /// The address the server should listen on.
    /// Use 0.0.0.0 (IPv4) or ::1 (IPv6) to listen to 
    /// all addresses.
    pub listen_address: String,
    /// The port the server should listen on.
    pub listen_port: u16,
    /// Map of slots being used by the server queue.
    /// The key (String) is the Queue/Room/Level/Map name.
    /// The value (u8) is the amount of slots per instance.
    pub slots: HashMap<String, u8>,
}

impl AppConfig {
    /// Loads a config from it's default path OR, if the config
    /// is missing, creates a default config on disk and returns
    /// it.
    pub fn load() -> Result<Self, Box<dyn Error>> {
        println!(
            "Config path: {}",
            confy::get_configuration_file_path("MatchMaker", None)?
                .to_str()
                .unwrap()
        );

        let app_config: AppConfig = confy::load("MatchMaker", None)?;

        println!("{}", app_config);

        Ok(app_config)
    }

    /// Converts the listen address and port into a string,
    /// like expected by WS-RS.
    pub fn listen_string(&self) -> String {
        format!("{}:{}", self.listen_address, self.listen_port)
    }
}

impl Default for AppConfig {
    fn default() -> Self {
        let mut slots = HashMap::new();
        slots.insert(String::from("Test"), 2);

        Self {
            listen_address: String::from("0.0.0.0"),
            listen_port: 33333,
            slots,
        }
    }
}

impl Display for AppConfig {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "AppConfig:
\tListen Address:\t{}
\tListen Port:\t{}
\tQueue Slot Config:
{}",
            self.listen_address,
            self.listen_port,
            self.slots
                .iter()
                .map(|(k, v)| format!("\t\t\"{}\": {} slots", k, v))
                .collect::<Vec<String>>()
                .join("\n")
        )
    }
}
