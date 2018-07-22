using System;
using System.Collections.Generic;
using System.Text;

namespace WhoreCoinDiscordPrototype {
	internal sealed class Server {

		internal bool RaffleActive {
			get; set;
		} = false;

		internal ulong RaffleChannelId {
			get; set;
		}

		internal readonly List<ulong> RaffleMembers = new List<ulong>();

		internal readonly Dictionary<ulong,ulong> BankAccounts = new Dictionary<ulong,ulong>();

		internal ulong GetBalance(ulong userId) {
			if(!BankAccounts.ContainsKey(userId)) {
				BankAccounts.Add(userId,0);
			}
			return BankAccounts[userId];
		}

		internal void AddBalance(ulong userId,ulong amount) {
			var balance = GetBalance(userId);
			if(amount > (ulong.MaxValue - balance)) {
				BankAccounts[userId] = ulong.MaxValue;
			} else {
				BankAccounts[userId] += amount;
			}
		}

		internal void RemoveBalance(ulong userId,ulong amount) {
			var balance = GetBalance(userId);
			if(amount > balance) {
				BankAccounts[userId] = ulong.MinValue;
			} else {
				BankAccounts[userId] -= amount;
			}
		}

	}
}
