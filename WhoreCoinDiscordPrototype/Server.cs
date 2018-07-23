using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WhoreCoinDiscordPrototype {
	internal sealed class Server {

		internal ulong Id {
			get; set;
		}
		private bool raffleActive = false;
		internal bool RaffleActive {
			get {
				return raffleActive;
			}
			set {
				raffleActive = value;
			}
		}

		internal ulong RaffleChannelId {
			get; set;
		}

		internal string FileDirectory {
			get; set;
		}

		internal readonly List<ulong> RaffleMembers = new List<ulong>();
		internal readonly Dictionary<ulong,ulong> BankAccounts = new Dictionary<ulong,ulong>();

		internal ulong GetBalance(ulong userId) {
			if(!BankAccounts.ContainsKey(userId)) {
				return ulong.MinValue;
			}
			return BankAccounts[userId];
		}

		internal async Task AddBalance(ulong userId,ulong amount) {
			var balance = GetBalance(userId);
			if(balance == ulong.MinValue) {
				BankAccounts.Add(userId,amount);
				await SaveBankAccount(userId);
				return;
			}
			if(balance == ulong.MaxValue) {
				return;
			}
			if(amount > (ulong.MaxValue - balance)) {
				BankAccounts[userId] = ulong.MaxValue;
			} else {
				BankAccounts[userId] += amount;
			}
			await SaveBankAccount(userId);
		}

		internal async Task RemoveBalance(ulong userId,ulong amount) {
			var balance = GetBalance(userId);
			if(balance == ulong.MinValue) {
				return;
			}
			if(amount > balance) {
				BankAccounts.Remove(userId);
			} else {
				BankAccounts[userId] -= amount;
			}
			await SaveBankAccount(userId);
		}

		internal async Task ClearBalance(ulong userId) {
			if(BankAccounts.ContainsKey(userId)) {
				BankAccounts.Remove(userId);
				await SaveBankAccount(userId);
			}
		}

		internal async Task AddRaffleMember(ulong userId) {
			RaffleMembers.Add(userId);
			await SaveRaffleList();
		}

		internal void LoadData() {
			var files = Directory.GetFiles(FileDirectory);
			var foundRaffle = false;
			foreach(var filePath in files) {
				var file = filePath.Split(Path.DirectorySeparatorChar)[2];
				if(!foundRaffle && file == "Raffle") {
					var raffleLines = File.ReadAllLines(Path.Combine(FileDirectory,"Raffle"));
					if(raffleLines.Length > 0 && ulong.TryParse(raffleLines[0],out ulong raffleChannelId)) {
						RaffleChannelId = raffleChannelId;
						for(var i = 1;i<raffleLines.Length;i++) {
							if(!ulong.TryParse(raffleLines[i],out ulong raffleId)) {
								continue;
							}
							RaffleMembers.Add(raffleId);
						}
						RaffleActive = true;
					}
					foundRaffle = true;
				}
				if(!ulong.TryParse(file,out ulong fileId)) {
					continue;
				}
				var fileText = File.ReadAllText(Path.Combine(FileDirectory,file));
				if(!ulong.TryParse(fileText,out ulong fileData)) {
					continue;
				}
				BankAccounts.Add(fileId,fileData);
			}
		}


		internal async Task SaveRaffleList() {
			var path = Path.Combine(FileDirectory,"Raffle");
			if(!RaffleActive) {
				if(File.Exists(path)) {
					File.Delete(path);
				}
			} else {
				List<string> lines = new List<string>() {
					RaffleChannelId.ToString()
				};
				foreach(var user in RaffleMembers) {
					lines.Add(user.ToString());
				}
				Directory.CreateDirectory(FileDirectory);
				await File.WriteAllLinesAsync(path,lines);

			}
		}

		private async Task SaveBankAccount(ulong userId) {
			var balance = GetBalance(userId);
			var path = Path.Combine(FileDirectory,userId.ToString());
			if(balance == 0) {
				if(File.Exists(path)) {
					File.Delete(path);
				}
			} else {
				Directory.CreateDirectory(FileDirectory);
				await File.WriteAllTextAsync(path,balance.ToString());
			}
		}

	}
}
