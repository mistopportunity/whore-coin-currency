using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace WhoreCoinDiscordPrototype {

	internal sealed class Program {

		private const string CommandPrefix = "!whore";
		private const int RaffleMultiplier = 5;

		private static readonly Random random = new Random();

		private DiscordSocketClient client;

		private static void Main(string[] args) {

			//Database setup and loading


			new Program().MainAsync().GetAwaiter().GetResult();
		}
		

		public async Task MainAsync() {

			client = new DiscordSocketClient();
			client.Log += LogAsync;
			client.Ready += ReadyAsync;
			client.MessageReceived += MessageReceivedAsync;

			await client.LoginAsync(
				TokenType.Bot,
				Environment.GetEnvironmentVariable("token")
			);

			await client.StartAsync();
			await Task.Delay(-1);
		}

		private Task LogAsync(LogMessage log) {
			Console.WriteLine(log.ToString());
			return Task.CompletedTask;
		}
		private Task ReadyAsync() {
			Console.WriteLine($"{client.CurrentUser} is connected!");
			return Task.CompletedTask;
		}
		private async Task MessageReceivedAsync(SocketMessage message) {

			//Let's not respond to ourself
			if(message.Author.Id == client.CurrentUser.Id) {
				return;
			}
			
			//Let's not respond to something if it has an attachment
			if(message.Attachments.Count > 0) {
				return;
			}
			if(message.Channel is SocketTextChannel) {
				if(message.Content.StartsWith(CommandPrefix)) {
					if(message.Author is SocketGuildUser) {

						var splitMessage = message.Content.Split(" ");
						if(splitMessage[0] != CommandPrefix) {
							return;
						}

						var user = message.Author as SocketGuildUser;
						var userId = user.Id;

						var isSuperAdmin = Environment.GetEnvironmentVariable("superadmin") == userId.ToString();

						var isAdmin = isSuperAdmin || user.GuildPermissions.Administrator;

						var channel = message.Channel as SocketTextChannel;
						var channelId = channel.Id;
						var guildId = channel.Guild.Id;

						var server = ServerManager.GetServer(guildId);

						switch(splitMessage[1]) {

							case "raffle":

								switch(splitMessage[2]) {

									case "progress":

										if(!server.RaffleActive) {
											await channel.SendMessageAsync($"No raffle is in progress at this time. Only an admin can start a raffle");
										} else {
											var enteredMembers = server.RaffleMembers.Count;

											await channel.SendMessageAsync($"{enteredMembers} user{(enteredMembers != 1 ? "s have" : " has")} joined the raffle so far");

										}
										break;
								
									case "start":

										if(isAdmin) {
											if(!server.RaffleActive) {
												await channel.SendMessageAsync($"A raffle has started! To enter, do {CommandPrefix} raffle enter");
												server.RaffleChannelId = channelId;
												server.RaffleActive = true;
											} else {
												await channel.SendMessageAsync("There is already an active raffle");
											}
										} else {
											await channel.SendMessageAsync("You are not an admin");
										}
										break;

									case "end":

										if(isAdmin) {
											if(server.RaffleActive) {

												if(server.RaffleMembers.Count < 2) {
													await channel.SendMessageAsync($"Sorry, not enough people {server.RaffleMembers.Count} joined the raffle (3 or more needed)");
												} else {

													var winnerIndex = random.Next(0,server.RaffleMembers.Count);
													var winnerUser = client.GetUser(server.RaffleMembers[winnerIndex]);
													var raffleChannel = client.GetChannel(server.RaffleChannelId);

													var winnings = RaffleMultiplier * server.RaffleMembers.Count;
													server.AddBalance(winnerUser.Id,(ulong)winnings);

													await ((SocketTextChannel)raffleChannel).SendMessageAsync($"Congratulations {winnerUser.Mention}! You won {winnings} whore coins from the raffle!");

													server.RaffleMembers.Clear();
													server.RaffleActive = false;
												}

											} else {
												await channel.SendMessageAsync($"There isn't currently a raffle. First, try !{CommandPrefix} raffle start");
											}
										} else {
											await channel.SendMessageAsync("You are not an admin");
										}
										break;
									case "enter":
										if(server.RaffleActive) {
											if(!server.RaffleMembers.Contains(userId)) {
												if(server.RaffleChannelId == channelId) {
													server.RaffleMembers.Add(userId);
													await channel.SendMessageAsync($"{user.Mention} joined the raffle!");
												} else {
													server.RaffleMembers.Add(userId);
													await channel.SendMessageAsync($"Entered raffle at <#{server.RaffleChannelId}>");
													var raffleChannel = client.GetChannel(server.RaffleChannelId);
													await ((SocketTextChannel)raffleChannel).SendMessageAsync($"{user.Mention} joined the raffle!");
												}
											} else {
												await channel.SendMessageAsync("You are already in the raffle!");
											}
										} else {
											await channel.SendMessageAsync("There is no raffle at this time");
										}
										break;
									
								}


								break;

							case "povertyhammer":


								if(isAdmin) {

									if(message.MentionedUsers.Count == 0) {
										if(splitMessage.Length == 2) {
											await channel.SendMessageAsync("The poverty hammer is not to be taken lightly");
										}
									} else {
										StringBuilder builder = new StringBuilder();
										foreach(var mentionedUser in message.MentionedUsers) {
											builder.AppendLine($"{mentionedUser.Username} was hit with the poverty hammer!");
											var balance = server.GetBalance(mentionedUser.Id);
											if(balance == 0) {
												continue;
											}
											server.RemoveBalance(mentionedUser.Id,balance);
										}
										await channel.SendMessageAsync(builder.ToString());
									}

								} else {
									await channel.SendMessageAsync("This command is for big boys. Maybe you should donate your money to charity instead?");
								}

								break;


							case "steal":

								if(isAdmin) {

									if(!ulong.TryParse(splitMessage[2],out ulong amount)) {

										await channel.SendMessageAsync("What kind of number is that?");

									} else if(amount == 0) {

										await channel.SendMessageAsync("You can't steal nothing!");

									} else {


										if(message.MentionedUsers.Count > 0) {

											bool stoleFromSomeone = false;
											StringBuilder builder = new StringBuilder();

											var subamount = amount / (ulong)message.MentionedUsers.Count;
											foreach(var mentionedUser in message.MentionedUsers) {

												if(mentionedUser.Id == user.Id) {
													continue;
												}

												var victimBalance = server.GetBalance(mentionedUser.Id);
												if(victimBalance == 0) {
													continue;
												}

												stoleFromSomeone = true;

												if(victimBalance < subamount) {
													subamount = victimBalance;
												}

												server.RemoveBalance(mentionedUser.Id,subamount);

												server.AddBalance(userId,subamount);

												builder.AppendLine($"Stole {subamount} whore coin{(subamount != 1 ? "s" : string.Empty)} from {mentionedUser.Username}");

											}

											if(stoleFromSomeone) {
												await channel.SendMessageAsync(builder.ToString());
											} else {
												await channel.SendMessageAsync("None of these users could be stolen from");
											}


										} else {
											await channel.SendMessageAsync("There's no one to steal from here");
										}



									}


								} else {
									await channel.SendMessageAsync("Only the government can steal whore coins");
								}

								break;

							case "balance":

								if(message.MentionedUsers.Count == 0) {
									if(splitMessage.Length == 2) {
										ulong balance = server.GetBalance(userId);
										await channel.SendMessageAsync($"You have {balance} whore coin{(balance != 1 ? "s" : string.Empty)}");
									}
								} else if(message.MentionedUsers.Count == 1) {
									foreach(var mentionedUser in message.MentionedUsers) {
										var balance = server.GetBalance(mentionedUser.Id);
										if(mentionedUser.Id == userId) {
											await channel.SendMessageAsync($"You have {balance} whore coin{(balance != 1 ? "s" : string.Empty)}");
										} else {
											await channel.SendMessageAsync($"{mentionedUser.Username} has {balance} whore coin{(balance != 1 ? "s" : string.Empty)}");
										}
									}
								} else {
									StringBuilder builder = new StringBuilder();
									foreach(var mentionedUser in message.MentionedUsers) {
										var balance = server.GetBalance(mentionedUser.Id);
										builder.AppendLine($"{mentionedUser.Username} has {balance} whore coin{(balance != 1 ? "s" : string.Empty)}");
									}
									await channel.SendMessageAsync(builder.ToString());
								}


								break;

							case "give":

								if(message.MentionedUsers.Count > 0) {
									if(message.MentionedUsers.Count == 1) {
										foreach(var mentionedUser in message.MentionedUsers) {
											if(mentionedUser.Id == userId) {
												await channel.SendMessageAsync("You can't give yourself money you lonely idiot");
											} else {

												if(!ulong.TryParse(splitMessage[3],out ulong amount)) {

													await channel.SendMessageAsync("What kind of number is that? Maybe try again after you finish your primary education");

												} else if(amount == 0) {

													await channel.SendMessageAsync($"I know you don't like {mentionedUser.Username}, but you can't give them 0 coins");

												} else {

													var userBalance = server.GetBalance(userId);

													if(userBalance >= amount) {

														server.RemoveBalance(userId,amount);
														server.AddBalance(mentionedUser.Id,amount);

														await channel.SendMessageAsync($"{user.Username} gave {mentionedUser.Username} {amount} whore coin{(amount != 1 ? "s" : string.Empty)}. How kind of them!");



													} else {
														await channel.SendMessageAsync("You cannot afford this transaction");
													}

												}
											}
										}
									} else {
										await channel.SendMessageAsync("You can only give money to one person at a time");
									}
								} else {
									await channel.SendMessageAsync("You can't give money to no one");
								}

								break;

							case "rollthatdough":
								if(isAdmin) {

									if(!ulong.TryParse(splitMessage[2],out ulong amount)) {

										await channel.SendMessageAsync("What kind of number is that?");

									} else if(amount == 0) {

										await channel.SendMessageAsync("You can't distribute nothing!");

									} else {

										if(message.MentionedUsers.Count > 0) {
											StringBuilder builder = new StringBuilder();
											var subamount = amount / (ulong)message.MentionedUsers.Count;

											foreach(var mentionedUser in message.MentionedUsers) {
												server.AddBalance(mentionedUser.Id,subamount);
												builder.AppendLine($"Gave {mentionedUser.Username} {subamount} whore coin{(subamount != 1 ? "s" : string.Empty)}");

											}

											await channel.SendMessageAsync(builder.ToString());
										} else {
											server.AddBalance(userId,amount);
											await channel.SendMessageAsync($"Gave {user.Username} {amount} whore coin{(amount != 1 ? "s" : string.Empty)}");
										}

									}



								} else {
									await channel.SendMessageAsync("You can't just go creating money that you don't have");
								}

								break;
							case "topwhores":

								if(server.BankAccounts.Count < 1) {
									return;
								} else {
									List<Tuple<ulong,ulong>> leaderboard = new List<Tuple<ulong,ulong>>();

									foreach(var bankAccount in server.BankAccounts) {
										leaderboard.Add(new Tuple<ulong,ulong>(bankAccount.Key,bankAccount.Value));
									}

									leaderboard.Sort((x,y) => y.Item2.CompareTo(x.Item2));

									var end = (leaderboard.Count > 10 ? 10 : leaderboard.Count);

									var builder = new StringBuilder();

									for(int i = 0;i<end;i++) {

										var balance = server.GetBalance(leaderboard[i].Item1);

										builder.AppendLine($"{i+1}. {client.GetUser(leaderboard[i].Item1)} - {balance} whore coin{(balance != 1 ? "s" : string.Empty)}");

									}

									await channel.SendMessageAsync(builder.ToString());

								}

								break;

						}

					} else {
						Console.WriteLine("Unknown user type for this channel");
						return;
					}
					return;
				} else {
					return;
				}
			} else if(message.Channel is SocketDMChannel) {
				await message.Channel.SendMessageAsync($"To check your balance on a server, do {CommandPrefix} balance");
				return;
			} else {
				Console.WriteLine("Unknown message channel type");
				return;
			}
		}

	}
}
