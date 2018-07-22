using System;
using System.Collections.Generic;
using System.Text;

namespace WhoreCoinDiscordPrototype {
	internal static class ServerManager {

		private static readonly Dictionary<ulong,Server> servers = new Dictionary<ulong,Server>();

		internal static bool Initialize() {
			//Populate servers
			return false;
		}

		internal static bool SaveServers() {
			//Save servers to hard disk
			return false;
		}

		internal static Server GetServer(ulong guildId) {
			if(!servers.ContainsKey(guildId)) {
				servers.Add(guildId,new Server());
			}
			return servers[guildId];
		}

	}
}
