using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace WhoreCoinDiscordPrototype {
	internal static class ServerManager {

		private static readonly Dictionary<ulong,Server> servers = new Dictionary<ulong,Server>();

		internal static bool Initialize() {
			if(Directory.Exists("Servers")) {
				var serverDirectories = Directory.GetDirectories("Servers");
				foreach(var directory in serverDirectories) {

					if(!ulong.TryParse(
						directory.Split(Path.DirectorySeparatorChar)[1],
					out ulong id)) {
						continue;
					}

					var server = new Server() {
						Id = id,
						FileDirectory = directory
					};

					server.LoadData();

					servers.Add(id,server);

				}
			} else {
				Directory.CreateDirectory("Servers");
			}

			return true;
		}

		internal static Server GetServer(ulong guildId) {
			if(!servers.ContainsKey(guildId)) {
				servers.Add(guildId,new Server() {
					Id = guildId,
					FileDirectory = Path.Combine("Servers",guildId.ToString())
				});
			}
			return servers[guildId];
		}

	}
}
