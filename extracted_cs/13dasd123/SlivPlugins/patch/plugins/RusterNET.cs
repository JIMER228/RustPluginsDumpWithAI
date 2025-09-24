/* 
                                                                                                                                                                                        
 ____            _            _   _ _____ _____                                                                                                                                         
|  _ \ _   _ ___| |_ ___ _ __| \ | | ____|_   _|                                                                                                                                        
| |_) | | | / __| __/ _ \ '__|  \| |  _|   | |                                                                                                                                          
|  _ <| |_| \__ \ ||  __/ |_ | |\  | |___  | |                                                                                                                                          
|_| \_\\__,_|___/\__\___|_(_)|_| \_|_____| |_|                                                                                                                                          
                                                                                                                                                                                        
       DO             NOT           MODIFY                                                                                                                                              
               NOR           EDIT                                                                                                                                                       
                    THANKS!                                                                                                                                                             
 */

/*                                                                                                                                                                                      
End-User License Agreement (EULA) of Ruster.NET
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This End-User License Agreement ("EULA") is a legal agreement between you and Raul-Sorin Sorban.                                                                                        
This EULA agreement governs your acquisition and use of our Ruster.NET software ("Software") directly from Raul-Sorin Sorban or indirectly through a Raul-Sorin Sorban authorized reseller or distributor (a "Reseller").
Please read this EULA agreement carefully before completing the installation process and using the Ruster.NET software. It provides a license to use the Ruster.NET software and contains warranty information and liability disclaimers.
If you register for a free trial of the Ruster.NET software, this EULA agreement will also govern that trial. By clicking "accept" or installing and/or using the Ruster.NET software, you are confirming your acceptance of the Software and agreeing to become bound by the terms of this EULA agreement.
                                                                                                                                                                                        
If you are entering into this EULA agreement on behalf of a company or other legal entity, you represent that you have the authority to bind such entity and its affiliates to these terms and conditions. If you do not have such authority or if you do not agree with the terms and conditions of this EULA agreement, do not install or use the Software, and you must not accept this EULA agreement.
This EULA agreement shall apply only to the Software supplied by Raul-Sorin Sorban herewith regardless of whether other software is referred to or described herein. The terms also apply to any Raul-Sorin Sorban updates, supplements, Internet-based services, and support services for the Software, unless other terms accompany those items on delivery. If so, those terms apply.
                                                                                                                                                                                        
License Grant                                                                                                                                                                           
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Raul-Sorin Sorban hereby grants you a personal, non-transferable, non-exclusive licence to use the Ruster.NET software on your devices in accordance with the terms of this EULA agreement.
You are permitted to load the Ruster.NET software (for example a PC, laptop, mobile or tablet) under your control.                                                                      
You are responsible for ensuring your device meets the minimum requirements of the Ruster.NET software.                                                                                 
                                                                                                                                                                                        
You are not permitted to:                                                                                                                                                               
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
* Edit, alter, modify, adapt, translate or otherwise change the whole or any part of the Software nor permit the whole or any part of the Software to be combined with or become incorporated in any other software, nor decompile, disassemble or reverse engineer the Software or attempt to do any such things.
* Reproduce, copy, distribute, resell or otherwise use the Software for any commercial purpose.                                                                                         
* Allow any third party to use the Software on behalf of or for the benefit of any third party.                                                                                         
* Use the Software in any way which breaches any applicable local, national or international law.                                                                                       
* Use the Software for any purpose that Raul-Sorin Sorban considers is a breach of this EULA agreement.                                                                                 
* Intellectual Property and Ownership                                                                                                                                                   
                                                                                                                                                                                        
Raul-Sorin Sorban shall at all times retain ownership of the Software as originally downloaded by you and all subsequent downloads of the Software by you.                              
The Software (and the copyright, and other intellectual property rights of whatever nature in the Software, including any modifications made thereto) are and shall remain the property of Raul-Sorin Sorban.
Raul-Sorin Sorban reserves the right to grant licences to use the Software to third parties.                                                                                            
                                                                                                                                                                                        
Termination                                                                                                                                                                             
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This EULA agreement is effective from the date you first use the Software and shall continue until terminated. You may terminate it at any time upon written notice to Raul-Sorin Sorban.
It will also terminate immediately if you fail to comply with any term of this EULA agreement. Upon such termination, the licenses granted by this EULA agreement will immediately terminate and you agree to stop all access and use of the Software. The provisions that by their nature continue and survive will survive any termination of this EULA agreement.
                                                                                                                                                                                        
Governing Law                                                                                                                                                                           
----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This EULA agreement, and any dispute arising out of or in connection with this EULA agreement, shall be governed by and construed in accordance with the laws of at.                    
*/

// Reference: RusterNET.Core
// Reference: System.Drawing
// Requires: GridAPI
// Requires: ImageLibrary

using CompanionServer;
using Facepunch;
using Humanlights;
using Humanlights.Components;
using Humanlights.Extensions;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
	[Info("RusterNET", "Raul-Sorin Sorban", "4.2.5")]
	[Description("Rust's first social-media network for your enemies and uncles.")]
	public class RusterNET : RustPlugin
	{
		public static RusterNET Instance { get; private set; }

		#region Version

		public string LatestVersion { get; set; }

		public void FetchLatestVersion(Action<string> onFetched)
		{
			webrequest.Enqueue("https://codefling.com/capi/file-867/?do=apiCall", string.Empty, (code, data) =>
			{
				if (string.IsNullOrEmpty(data)) return;
				var jObject = (JObject)null;

				try
				{
					jObject = JObject.Parse(data);
				}
				catch { return; }

				onFetched?.Invoke(jObject["file"]["file_version"].ToObject<string>());
			}, this);
		}

		public bool IsRunningLatestVersion()
		{
			return LatestVersion == Version.ToString();
		}

		#endregion

		#region Pro

		public JObject ProConfig { get; private set; }

		public bool IsPro()
		{
			return RusterNETPro != null && RusterNETPro.IsLoaded;
		}

		public T GetProConfig<T>(string property, T original)
		{
			if (ProConfig == null) return original;

			var value = ProConfig[property].Value<T>();
			if (value?.ToString() == "0") return original;
			return string.IsNullOrEmpty(value?.ToString()) ? original : value;
		}

		public void RefreshPro()
		{
			UnloadPro();

			ProConfig = RusterNETPro.Call<JObject>("GetConfig");

			if (!ProConfig["AppendEmojis"].ToString().ToBool()) Emojis.Clear();

			foreach (var emoji in ProConfig["Emojis"])
			{
				var name = emoji["Name"].ToString();
				var shortname = emoji["Shortname"].ToString();
				var iconUrl = emoji["IconUrl"].ToString();

				if (!string.IsNullOrEmpty(name) &&
					!string.IsNullOrEmpty(shortname) &&
					!string.IsNullOrEmpty(iconUrl))
					Emojis.Add(new RusterEmoji(name, shortname, iconUrl));
			}
		}
		public void UnloadPro()
		{
			Emojis.Clear();
			Emojis.AddRange(GetDefaultEmojis());

			ProConfig = null;
		}

		#endregion

		#region Adverts

		public const bool AdHourlyChange = true;
		public const int AdChance = 3;
		public const int AdPossiblities = 50;
		public const int AdInitialSeed = 103;
		public static int AdPlacementSeed
		{
			get
			{
				var date = DateTime.Now;
				var seed = AdInitialSeed + date.Year + date.Month + date.Day;
				if (AdHourlyChange) seed += date.Hour;
				return seed;
			}
		}

		#endregion

		#region Flea Market

		public Dictionary<string, int> DefaultStackSizes { get; set; } = new Dictionary<string, int>();

		public const string FleaGetRoute = @"http://167.86.121.152:8802/rusternet-ms?method=flea.get";
		public const string FleaBuyRoute = @"http://167.86.121.152:8802/rusternet-ms?method=flea.buy";
		public const string FleaVendorsRoute = @"http://167.86.121.152:8802/rusternet-ms?method=flea.vendors";
		public const string DefaultStacksizesUrl = @"https://raw.githubusercontent.com/raulssorban/rusternet-lang/main/default_stacksizes.json";
		public Dictionary<ulong, string[]> VendorPhrases { get; set; } = new Dictionary<ulong, string[]>();
		public TimeSince TimeSinceVendorFetch { get; set; } = 10f;

		public bool IsInFleaMarket(RusterBrowser browser)
		{
			return IsInternetOnline && browser.CustomFeed1 == FleaMarketId;
		}
		public RusterFeed GetFleaMarket()
		{
			if (TimeSinceVendorFetch > 5f)
			{
				webrequest.Enqueue(FleaVendorsRoute, string.Empty, (int statusCode, string data) =>
				{
					VendorPhrases.Clear();
					var vendors = JsonConvert.DeserializeObject<JToken>(data);

					foreach (var vendor in vendors)
					{
						var user = Data.GetUser(vendor["Id"].ToObject<ulong>());
						user.IsBot = true;
						user.CustomDisplayName = vendor["PublicName"].ToObject<string>();
						user.CurrentDisplayName = vendor["FullName"].ToObject<string>();
						user.AvatarUrl = vendor["AvatarUrl"].ToObject<string>();
						user.AllowBusinessCard = false;

						if (!VendorPhrases.ContainsKey(user.Id))
							VendorPhrases.Add(user.Id, vendor["Phrases"].ToObject<string[]>());
					}
				}, this, RequestMethod.GET);

				TimeSinceVendorFetch = 0f;
			}

			var fleaMarket = Data.GetFeed(FleaMarketId);
			{
				fleaMarket.Title = "Flea Market";
				fleaMarket.BackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/889202161689456670/ruster_fleamarket.png";
				fleaMarket.FeedType = RusterFeed.FeedTypes.Shop;
				fleaMarket.DarkMode = false;

				fleaMarket.EnableCensorship = false;
				fleaMarket.AllowAdverts = true;
				fleaMarket.AllowPurchases = true;
				fleaMarket.AllowRatings = false;
				fleaMarket.AllowRestocking = false;
				fleaMarket.CanFilter = false;
				fleaMarket.ShowHashtags = true;
				fleaMarket.ShowLocation = true;
				fleaMarket.ShowRatings = false;
				fleaMarket.ShowReplies = true;
				fleaMarket.ShowDate = false;
				fleaMarket.IsLocked = true;
				fleaMarket.Hashtags = null;
			}

			return fleaMarket;
		}
		public void FetchFleaMarket(Action<bool> onFetched = null)
		{
			var fleaMarket = GetFleaMarket();

			webrequest.Enqueue(FleaGetRoute,
				$@"{{ ""sessionId"": ""{(global::RusterNET.Core.Internet.GetSessionId())}"" }}",
				(int statusCode, string data) =>
				{
					if (statusCode != 200)
					{
						onFetched?.Invoke(false);
						return;
					}

					var flea = JsonConvert.DeserializeObject<JToken>(data);
					fleaMarket.Posts.Clear();

					foreach (var vendor in VendorPhrases)
					{
						var feed = Data.GetFeed(vendor.Key);
						feed.Posts.Clear();

						var count = flea.Count(x => x["VendorId"].ToString() == vendor.Key.ToString());
						var post = new RusterFeed.RusterPost()
						{
							Id = RusterFeed.RusterPost.GetId(),
							UserId = vendor.Key,
							Content = $"Premium Flea-Market seller. Open replies to browse {count:n0} {count.Plural("item", "items")} for sale.",
							CanRate = false,

							Ticks = DateTick.Current.Ticks,
							IsPinned = true
						};
						var postFeed = Data.GetFeed(post);
						postFeed.Posts.Clear();

						fleaMarket.Posts.Add(post);
					}

					foreach (var listing in flea)
					{
						var id = listing["Id"].ToObject<int>();
						var shortName = listing["ItemShortname"].ToObject<string>();
						var availableStack = listing["AvailableStack"].ToObject<int>();
						var totalStack = listing["TotalStack"].ToObject<int>();
						var definition = ItemManager.FindItemDefinition(shortName);
						var user = Data.GetUser(listing["VendorId"].ToObject<ulong>());
						var phrases = VendorPhrases.ContainsKey(user.Id) ? VendorPhrases[user.Id] : new string[] { "Problem?" };
						var phrase = phrases[RandomEx.GetRandomInteger(0, phrases.Length - 1, id)];
						var feed = Data.GetFeed(user.GetPosts()[0]);
						var post = new RusterFeed.RusterPost()
						{
							Id = id,
							UserId = user.Id,
							Content = string.Format(phrase, definition.displayName.english, $"{availableStack:n0}", $"{totalStack:n0}"),
							MarketplaceListing = new RusterMarketplaceListing
							{
								Shortname = shortName,
								Skin = listing["ItemSkin"].ToObject<ulong>(),
								Price = (int)(listing["Price"].ToObject<int>() * GetItemAmplifier(shortName)),
								Amount = totalStack,
								AmountLeft = availableStack,
								WholeStack = false,
								IsPurchased = availableStack <= 0
							},
							CanRate = false,
							Ticks = DateTick.Current.Ticks,
							IsPinned = false
						};
						feed.FeedType = RusterFeed.FeedTypes.Shop;
						feed.Title = $"{user.GetUsername()}'s Sales";
						feed.Posts.Add(post);
						feed.IsLocked = true;
						feed.ShowReplies = false;

						if (feed.Hashtags != null) feed.Hashtags.Clear();
						else feed.Hashtags = new List<RusterHashtag>();

						foreach (var item in feed.Posts)
						{
							if (feed.Hashtags.Any(x => x.Filter == item.MarketplaceListing.Shortname && x.FilterType == RusterHashtag.FilterTypes.ItemShortname)) continue;
							feed.Hashtags.Add(
								new RusterHashtag(item.MarketplaceListing.Shortname, RusterHashtag.FilterTypes.ItemShortname)
								{
									Instances = feed.Posts.Count(x => x.MarketplaceListing.Shortname == item.MarketplaceListing.Shortname)
								});
						}

						GetSteamWorkshopIcon(post.MarketplaceListing.Skin, url => { post.PhotoUrl = url; });
					}

					onFetched?.Invoke(true);
				}, this, RequestMethod.PUT);
		}
		public void GetVanillaStackSizes()
		{
			webrequest.Enqueue(DefaultStacksizesUrl, string.Empty, (int code, string data) =>
			{
				DefaultStackSizes = JsonConvert.DeserializeObject<Dictionary<string, int>>(data);
				Log($"Downloaded vanilla stack sizes.");
			}, this);
		}

		[ConsoleCommand("rusterdefaultstacksizes")]
		private void ExportVanillaStackSizes(ConsoleSystem.Arg arg)
		{
			// if (!arg.IsAdmin) return;
			// 
			// var bundles = AssetBundle.GetAllLoadedAssetBundles();
			// foreach (var b in bundles) Puts(b.name);
			// 
			// var root = bundles.FirstOrDefault(x => x.name == null || string.IsNullOrEmpty(x.name));
			// var bundle = bundles.FirstOrDefault(x => x.name.Contains("items.preload.bundle"));
			// 
			// var definitions = bundle.LoadAllAssets<GameObject>();
			// var originalItemDefinitions = (from x in definitions
			// 							   select x.GetComponent<ItemDefinition>() into x
			// 							   where x != null
			// 							   select x).ToList();
			// 
			// var dictionary = new Dictionary<string, int>();
			// foreach (var item in originalItemDefinitions) { dictionary.Add(item.shortname, item.stackable); }
			// 
			// OsEx.File.Create($"oxide/temp/default_stacksizes.json", JsonConvert.SerializeObject(dictionary, Formatting.Indented));
			// 
			// var appleOriginalDefinition = originalItemDefinitions.FirstOrDefault(x => x.shortname == "apple");
			// Puts(appleOriginalDefinition.stackable.ToString());
			// 
			// var appleModifiedDefinition = ItemManager.FindItemDefinition("apple");
			// Puts(appleModifiedDefinition.stackable.ToString());
		}

		public float GetItemAmplifier(string shortName)
		{
			var original = (float)DefaultStackSizes[shortName];
			var current = (float)ItemManager.FindItemDefinition(shortName).stackable;

			return current.Scale(original, 1000, 1, 37.5f);
		}

		#endregion

		#region (ma name) GIF

		public System.Drawing.Image[] GetFrames(System.Drawing.Image image, int everyFrame = 1)
		{
			var frameCount = image.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
			var frames = new System.Drawing.Image[frameCount];
			everyFrame = frameCount.Scale(15, 250, 0, 3);

			for (int i = 0; i < frameCount; i++)
			{
				if (everyFrame != 0 && i % everyFrame != 0) continue;

				image.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time, i);
				frames[i] = new System.Drawing.Bitmap(image);
			}

			image.Dispose();
			return frames;
		}

		#endregion

		#region Skins

		public const string RusterSkinName = "Ruster.NET";
		public const ulong RusterSkinId = 2751157264;

		public const string RusterMarketplace24hAdvertSkinName = "Ruster.NET 24h Advert";
		public const ulong RusterMarketplace24hAdvertSkinId = 2751522930;

		public const string RusterMarketplace1wAdvertSkinName = "Ruster.NET 1w Advert";
		public const ulong RusterMarketplace1wAdvertSkinId = 2751524225;

		public const string RusterBusinessCardSkinName = "{0}'s Ruster.NET Business Card";
		public const ulong RusterBusinessCardSkinId = 2751518631;

		public const string RusterShortFlipbookSkinName = "Ruster.NET Short Flipbook";
		public const ulong RusterShortFlipbookSkinId = 2741149446;

		public const string RusterMediumFlipbookSkinName = "Ruster.NET Medium Flipbook";
		public const ulong RusterMediumFlipbookSkinId = 2741150219;

		public const string RusterLongFlipbookSkinName = "Ruster.NET Long Flipbook";
		public const ulong RusterLongFlipbookSkinId = 2741150823;

		public const string RusterLegitLotteryTicketSkinName = "Ruster.NET Legit Lottery Ticket";
		public const ulong RusterLegitLotteryTicketSkinId = 2747116980;

		public const string RusterLuckyCharmLotteryTicketSkinName = "Ruster.NET Lucky-Charm Lottery Ticket";
		public const ulong RusterLuckyCharmLotteryTicketSkinId = 2747116358;

		public const string RusterIconicLotteryTicketSkinName = "Ruster.NET Iconic Lottery Ticket";
		public const ulong RusterIconicLotteryTicketSkinId = 2747115474;

		public const string RusterGiftCardSkinName = "Ruster.NET {0} Gift Card";
		public const ulong RusterGiftCardSkinId = 2752332299;

		#endregion

		#region Install

		public const int CommunityFeedId = 0;
		public const int MarketplaceFeedId = 1;
		public const int UserStoreFeedId = 2;
		public const int LotteryStoreFeedId = 3;
		public const int BlackmarketFeedId = 666;
		public const int RedRoomFeedId = 667;
		public const int FleaMarketId = 668;
		public const int ReportGroupId = -400;

		public const int CodeflingBotId = 6969;

		protected WebRequests WebRequest = new WebRequests();

		public void InstallEventCommands()
		{
			Log($"Installing CUI commands...");

			cmd.AddConsoleCommand(LanguageDialogCmd, this, nameof(LanguageDialog));
			cmd.AddConsoleCommand(LanguageDialogChangeCmd, this, nameof(LanguageDialogChange));
			cmd.AddConsoleCommand(LanguageDialogCloseCmd, this, nameof(LanguageDialogClose));
			cmd.AddConsoleCommand(WithdrawCmd, this, nameof(Withdraw));
			cmd.AddConsoleCommand(RestockAllCmd, this, nameof(RestockAll));
			cmd.AddConsoleCommand(MainFeedChangeCmd, this, nameof(MainFeedChange));
			cmd.AddConsoleCommand(MainDMsCmd, this, nameof(MainDMs));
			cmd.AddConsoleCommand(ChangeHashtagCmd, this, nameof(ChangeHashtag));
			cmd.AddConsoleCommand(HashtagFilterCmd, this, nameof(HashtagFilter));
			cmd.AddConsoleCommand(HashtagFilterSubmitCmd, this, nameof(HashtagFilterSubmit));
			cmd.AddConsoleCommand(HashtagFilterCancelCmd, this, nameof(HashtagFilterCancel));
			cmd.AddConsoleCommand(HashtagFilterChangeCmd, this, nameof(HashtagFilterChange));
			cmd.AddConsoleCommand(AddFriendCmd, this, nameof(AddFriend));
			cmd.AddConsoleCommand(CancelFriendRequestCmd, this, nameof(CancelFriendRequest));
			cmd.AddConsoleCommand(HandleFriendRequestCmd, this, nameof(HandleFriendRequest));
			cmd.AddConsoleCommand(RemoveFriendCmd, this, nameof(RemoveFriend));
			cmd.AddConsoleCommand(CloseCmd, this, nameof(Close));
			cmd.AddConsoleCommand(LikeCmd, this, nameof(Like));
			cmd.AddConsoleCommand(DislikeCmd, this, nameof(Dislike));
			cmd.AddConsoleCommand(VoteCmd, this, nameof(Vote));
			cmd.AddConsoleCommand(NewPostCmd, this, nameof(NewPost));
			cmd.AddConsoleCommand(NewPostCloseCmd, this, nameof(NewPostClose));
			cmd.AddConsoleCommand(NewPostContentCmd, this, nameof(NewPostContent));
			cmd.AddConsoleCommand(NewPostPublishCmd, this, nameof(NewPostPublish));
			cmd.AddConsoleCommand(NewPostAddPictureCmd, this, nameof(NewPostAddPicture));
			cmd.AddConsoleCommand(NewPostAddCassetteCmd, this, nameof(NewPostAddCassette));
			cmd.AddConsoleCommand(NewPostUploadAudioCmd, this, nameof(NewPostUploadAudio));
			cmd.AddConsoleCommand(NewPostUploadAudioCloseCmd, this, nameof(NewPostUploadAudioClose));
			cmd.AddConsoleCommand(NewPostUploadAudioUploadCmd, this, nameof(NewPostUploadAudioUpload));
			cmd.AddConsoleCommand(NewPostUploadAudioTitleChangeCmd, this, nameof(NewPostUploadAudioTitleChange));
			cmd.AddConsoleCommand(NewPostUploadAudioStartTimeChangeCmd, this, nameof(NewPostUploadAudioStartTimeChange));
			cmd.AddConsoleCommand(NewPostUploadAudioUrlChangeCmd, this, nameof(NewPostUploadAudioUrlChange));
			cmd.AddConsoleCommand(NewPostLocationCmd, this, nameof(NewPostLocation));
			cmd.AddConsoleCommand(NewPostPollCmd, this, nameof(NewPostPoll));
			cmd.AddConsoleCommand(NewPostPollCloseCmd, this, nameof(NewPostPollClose));
			cmd.AddConsoleCommand(NewPostPollClearCmd, this, nameof(NewPostPollClear));
			cmd.AddConsoleCommand(NewPostPollDurationCmd, this, nameof(NewPostPollDuration));
			cmd.AddConsoleCommand(NewPostPollAddChoiceCmd, this, nameof(NewPostPollAddChoice));
			cmd.AddConsoleCommand(NewPostPollAddChoiceMoveUpCmd, this, nameof(NewPostPollAddChoiceMoveUp));
			cmd.AddConsoleCommand(NewPostPollAddChoiceMoveDownCmd, this, nameof(NewPostPollAddChoiceMoveDown));
			cmd.AddConsoleCommand(NewPostPollAddChoiceDeleteCmd, this, nameof(NewPostPollAddChoiceDelete));
			cmd.AddConsoleCommand(NewPostSoldWholeStackCmd, this, nameof(NewPostSoldWholeStack));
			cmd.AddConsoleCommand(NewPostSoldItemCmd, this, nameof(NewPostSoldItem));
			cmd.AddConsoleCommand(NewPostSoldRemoveCmd, this, nameof(NewPostSoldRemove));
			cmd.AddConsoleCommand(NewPostSoldPriceChangeCmd, this, nameof(NewPostSoldPriceChange));
			cmd.AddConsoleCommand(NewPostRecordVoiceCmd, this, nameof(NewPostRecordVoice));
			cmd.AddConsoleCommand(FullPostCmd, this, nameof(FullPost));
			cmd.AddConsoleCommand(CloseFullPostCmd, this, nameof(CloseFullPost));
			cmd.AddConsoleCommand(BuyPostCmd, this, nameof(BuyPost));
			cmd.AddConsoleCommand(RestockPostCmd, this, nameof(RestockPost));
			cmd.AddConsoleCommand(PinPostCmd, this, nameof(PinPost));
			cmd.AddConsoleCommand(DeleteCmd, this, nameof(Delete));
			cmd.AddConsoleCommand(NextPageCmd, this, nameof(NextPage));
			cmd.AddConsoleCommand(PrevPageCmd, this, nameof(PrevPage));
			cmd.AddConsoleCommand(StartPageCmd, this, nameof(StartPage));
			cmd.AddConsoleCommand(EndPageCmd, this, nameof(EndPage));
			cmd.AddConsoleCommand(SetPageCmd, this, nameof(SetPage));
			cmd.AddConsoleCommand(CloseNoticeCmd, this, nameof(CloseNotice));
			cmd.AddConsoleCommand(ReadNoticeCmd, this, nameof(ReadNotice));
			cmd.AddConsoleCommand(ProfileCmd, this, nameof(Profile));
			cmd.AddConsoleCommand(CloseProfileCmd, this, nameof(CloseProfile));
			cmd.AddConsoleCommand(CreateProfileCardCmd, this, nameof(CreateProfileCard));
			cmd.AddConsoleCommand(BlockCmd, this, nameof(Block));
			cmd.AddConsoleCommand(DMCmd, this, nameof(DM));
			cmd.AddConsoleCommand(TradeCmd, this, nameof(Trade));
			cmd.AddConsoleCommand(SkinPreviewCmd, this, nameof(SkinPreview));
			cmd.AddConsoleCommand(ReadNotificationCmd, this, nameof(ReadNotification));
			cmd.AddConsoleCommand(ReadAllNotificationsCmd, this, nameof(ReadAllNotifications));
			cmd.AddConsoleCommand(DeleteNotificationCmd, this, nameof(DeleteNotification));
			cmd.AddConsoleCommand(ChangeConversationCmd, this, nameof(ChangeConversation));
			cmd.AddConsoleCommand(ConversationMessageChangeCmd, this, nameof(ConversationMessageChange));
			cmd.AddConsoleCommand(ConversationSendCmd, this, nameof(ConversationSend));
			cmd.AddConsoleCommand(ConversationSendLocationCmd, this, nameof(ConversationSendLocation));
			cmd.AddConsoleCommand(ConversationMessageDeleteCmd, this, nameof(ConversationMessageDelete));
			cmd.AddConsoleCommand(ConversationDeleteCmd, this, nameof(ConversationDelete));
			cmd.AddConsoleCommand(ChangeMessageReactionCmd, this, nameof(ChangeMessageReaction));
			cmd.AddConsoleCommand(UpdateMessageReactionCmd, this, nameof(UpdateMessageReaction));
			cmd.AddConsoleCommand(CloseMessageReactionCmd, this, nameof(CloseMessageReaction));
			cmd.AddConsoleCommand(AcceptConfirmDialogCmd, this, nameof(AcceptConfirmDialog));
			cmd.AddConsoleCommand(CloseConfirmDialogCmd, this, nameof(CloseConfirmDialog));
			cmd.AddConsoleCommand(ConfigDMNotificationsCmd, this, nameof(ConfigDMNotifications));
			cmd.AddConsoleCommand(ConfigPushNotificationsCmd, this, nameof(ConfigPushNotifications));
			cmd.AddConsoleCommand(ConfigFriendsNotificationsCmd, this, nameof(ConfigFriendsNotifications));
			cmd.AddConsoleCommand(ConfigRustPlusNotificationsCmd, this, nameof(ConfigRustPlusNotifications));
			cmd.AddConsoleCommand(CurrentStackChangeCmd, this, nameof(CurrentStackChange));
			cmd.AddConsoleCommand(PlayPostCmd, this, nameof(PlayPost));
			cmd.AddConsoleCommand(PlayMessageCmd, this, nameof(PlayMessage));
			cmd.AddConsoleCommand(StopPostCmd, this, nameof(StopPost));
			cmd.AddConsoleCommand(PinToggleCmd, this, nameof(PinToggle));
			cmd.AddConsoleCommand(EditPostCmd, this, nameof(EditPost));
			cmd.AddConsoleCommand(PinNotificationTrayToggleCmd, this, nameof(PinNotificationTrayToggle));
			cmd.AddConsoleCommand(PostLikesDislikesCmd, this, nameof(PostLikesDislikes));
			cmd.AddConsoleCommand(ClosePostLikesDislikesCmd, this, nameof(ClosePostLikesDislikes));
			cmd.AddConsoleCommand(OpenServerViewerCmd, this, nameof(OpenServerViewer));
			cmd.AddConsoleCommand(CloseViewedServerCmd, this, nameof(CloseViewedServer));
			cmd.AddConsoleCommand(OpenViewedServerCmd, this, nameof(OpenViewedServer));
			cmd.AddConsoleCommand(CloseServerViewerCmd, this, nameof(CloseServerViewer));
			cmd.AddConsoleCommand(OpenStoryCmd, this, nameof(OpenStory));
			cmd.AddConsoleCommand(CloseStoryCmd, this, nameof(CloseStory));
			cmd.AddConsoleCommand(CreateStoryCmd, this, nameof(CreateStory));
			cmd.AddConsoleCommand(DeleteStoryCmd, this, nameof(DeleteStory));
			cmd.AddConsoleCommand(OpenPictureViewerCmd, this, nameof(OpenPictureViewer));
			cmd.AddConsoleCommand(ClosePictureViewerCmd, this, nameof(ClosePictureViewer));
			cmd.AddConsoleCommand(CloseTextEditorCmd, this, nameof(CloseTextEditor));
			cmd.AddConsoleCommand(TextEditorContentCmd, this, nameof(TextEditorContent));
			cmd.AddConsoleCommand(OpenContactsCmd, this, nameof(OpenContacts));
			cmd.AddConsoleCommand(CloseContactsCmd, this, nameof(CloseContacts));
			cmd.AddConsoleCommand(ChangeContactsFilterCmd, this, nameof(ChangeContactsFilter));
			cmd.AddConsoleCommand(NewGroupCmd, this, nameof(NewGroup));
			cmd.AddConsoleCommand(GroupAddCmd, this, nameof(GroupAdd));
			cmd.AddConsoleCommand(GroupPickCmd, this, nameof(GroupPick));
			cmd.AddConsoleCommand(GroupKickCmd, this, nameof(GroupKick));
			cmd.AddConsoleCommand(FleaMarketCmd, this, nameof(FleaMarket));
			cmd.AddConsoleCommand(OpenUserSettingsCmd, this, nameof(OpenUserSettings));
			cmd.AddConsoleCommand(CloseUserSettingsCmd, this, nameof(CloseUserSettings));
			cmd.AddConsoleCommand(UpdateUserSettingsCmd, this, nameof(UpdateUserSettings));
			cmd.AddConsoleCommand(CloseColorPickerCmd, this, nameof(CloseColorPicker));
			cmd.AddConsoleCommand(PickColorPickerCmd, this, nameof(PickColorPicker));
			cmd.AddConsoleCommand(ReportPostCmd, this, nameof(ReportPost));
			cmd.AddConsoleCommand(ReportUserCmd, this, nameof(ReportUser));
			cmd.AddConsoleCommand(OpenCouponEditorCmd, this, nameof(OpenCouponEditor));
			cmd.AddConsoleCommand(CloseCouponEditorCmd, this, nameof(CloseCouponEditor));
			cmd.AddConsoleCommand(SaveCouponEditorCmd, this, nameof(SaveCouponEditor));
			cmd.AddConsoleCommand(ClearCouponEditorCmd, this, nameof(ClearCouponEditor));
			cmd.AddConsoleCommand(SetOptionCouponEditorCmd, this, nameof(SetOptionCouponEditor));
			cmd.AddConsoleCommand(OpenCouponListCmd, this, nameof(OpenCouponList));
			cmd.AddConsoleCommand(CloseCouponListCmd, this, nameof(CloseCouponList));
			cmd.AddConsoleCommand(AddCouponListCmd, this, nameof(AddCouponList));
			cmd.AddConsoleCommand(RemoveCouponListCmd, this, nameof(RemoveCouponList));
			cmd.AddConsoleCommand(OpenTransactionListCmd, this, nameof(OpenTransactionList));
			cmd.AddConsoleCommand(CloseTransactionListCmd, this, nameof(CloseTransactionList));
			cmd.AddConsoleCommand(SwitchViewTransactionListCmd, this, nameof(SwitchViewTransactionList));
			cmd.AddConsoleCommand(RemoveTransactionListCmd, this, nameof(RemoveTransactionList));
			cmd.AddConsoleCommand(OpenPostGifPanelCmd, this, nameof(OpenPostGifPanel));
			cmd.AddConsoleCommand(CloseGifPanelCmd, this, nameof(CloseGifPanel));
			cmd.AddConsoleCommand(NewPostAddGIFCmd, this, nameof(NewPostAddGIF));
			cmd.AddConsoleCommand(UserAvatarCmd, this, nameof(UserAvatar));
			cmd.AddConsoleCommand(SelectUserPhotoListCmd, this, nameof(SelectAvatarList));
			cmd.AddConsoleCommand(CloseUserPhotoListCmd, this, nameof(CloseAvatarList));
			cmd.AddConsoleCommand(OpenBannerListCmd, this, nameof(OpenBannerList));
			cmd.AddConsoleCommand(OpenAvatarListCmd, this, nameof(OpenAvatarList));
			cmd.AddConsoleCommand(OpenFrameListCmd, this, nameof(OpenFrameList));
			cmd.AddConsoleCommand(EditAboutMeCmd, this, nameof(EditAboutMe));
			cmd.AddConsoleCommand(OpenStoreCmd, this, nameof(OpenStore));
			cmd.AddConsoleCommand(SelectUserUsersCmd, this, nameof(SelectUserUsers));
			cmd.AddConsoleCommand(CloseUsersCmd, this, nameof(CloseUsers));
			cmd.AddConsoleCommand(OpenGiftBasketCmd, this, nameof(OpenGiftBasket));
			cmd.AddConsoleCommand(ConversationTradeCmd, this, nameof(ConversationTrade));
			cmd.AddConsoleCommand(CloseModalCmd, this, nameof(CloseModal));
			cmd.AddConsoleCommand(SubmitModalCmd, this, nameof(SubmitModal));
			cmd.AddConsoleCommand(ValueModalCmd, this, nameof(ValueModal));
			cmd.AddConsoleCommand(ResetValueModalCmd, this, nameof(ResetValueModal));

		}
		public void InstallUserCommands()
		{
			Log($"Installing user commands...");

			cmd.AddConsoleCommand(Config.Commands.RusterCommand, this, nameof(Ruster));
			cmd.AddChatCommand(Config.Commands.RusterLoginCommand, this, nameof(RusterLogin));
			cmd.AddChatCommand(Config.Commands.LaunchRuster, this, nameof(LaunchRuster));
			cmd.AddConsoleCommand(Config.Commands.LaunchRuster, this, nameof(LaunchRuster));
			cmd.AddChatCommand(Config.Commands.CloseRuster, this, nameof(CloseRuster));
			cmd.AddChatCommand(Config.Commands.GetRuster, this, nameof(GetRuster));
			cmd.AddChatCommand(Config.Commands.Get24hAdvert, this, nameof(GetRuster24hAdvert));
			cmd.AddChatCommand(Config.Commands.Get1wAdvert, this, nameof(GetRuster1wAdvert));
			cmd.AddChatCommand(Config.Commands.RusterAllNotifications, this, nameof(RusterAllNotifications));
			cmd.AddChatCommand(Config.Commands.RusterPushNotifications, this, nameof(RusterPushNotifications));
			cmd.AddChatCommand(Config.Commands.RusterFriendsNotifications, this, nameof(RusterFriendsNotifications));
			cmd.AddChatCommand(Config.Commands.RusterRustPlusNotifications, this, nameof(RusterRustPlusNotifications));
			cmd.AddChatCommand(Config.Commands.RusterChatNotifications, this, nameof(RusterChatNotifications));
			cmd.AddChatCommand(Config.Commands.PinRusterFM, this, nameof(PinRusterFM));
			cmd.AddChatCommand(Config.Commands.RusterRatio, this, nameof(RusterRatio));
		}
		public void InitializeAdvertValidator(bool doTimer = true)
		{
			if (doTimer) Log($"Initializing advert validator...");

			var action = new Action(() =>
			{
				foreach (var user in Data.Users)
				{
					try
					{
						foreach (var post in user.GetAdvertPosts())
						{
							try
							{
								if (post.IsOverdue())
								{
									var feed = post.GetFeed();
									var reason = "";

									if (Config.Marketplace.RefundOnExpiredAdvert)
										post.RefundListing();

									feed.Delete(user, post.Id, out reason);
								}
							}
							catch { }
						}
					}
					catch { }
				}
			});

			action?.Invoke();
			if (doTimer) timer.Every(ConVar.Server.saveinterval, action);
		}
		public void InitializeStoryValidator(bool doTimer = true)
		{
			if (doTimer) Log($"Initializing Story validator...");

			var action = new Action(() =>
			{
				foreach (var story in Data.Stories.ToArray())
				{
					try
					{
						if (story.IsExpired(overHours: 24))
						{
							var author = story.GetUser();
							author.CreateNotification(
								$"Your story has expired. It has reached {story.Views.Count:n0} {story.Views.Count.Plural("view", "views")}.",
								RusterUserNotification.NotificationTypes.Story);

							Data.DeleteStory(story.Id);
						}
					}
					catch { }
				}
			});

			action?.Invoke();
			if (doTimer) timer.Every(ConVar.Server.saveinterval, action);
		}
		public void InitializeLotteryValidator(bool doTimer = true)
		{
			if (doTimer) Log($"Initializing Lottery validator...");

			var action = new Action(() =>
			{
				if (LotteryEvent == null) return;

				var lottery = LotteryEvent;
				var timeLeft = Config.Lottery.EventDuration - lottery.TimeSinceStart;

				if (lottery.TimeSinceStart > Config.Lottery.EventDuration)
				{
					lottery.EndEvent();
					return;
				}

				if (timeLeft <= 0) return;
				if (timeLeft <= 10f)
				{
					Print($"Lottery results in <color=orange>{Humanlights.Extensions.TimeEx.Format(timeLeft, false).ToLower()}</color>.");
				}
				else if (timeLeft <= 30f)
				{
					Puts($"{(int)timeLeft % 5}");
					if ((int)timeLeft % 5 == 0) Print($"Lottery results in <color=orange>{Humanlights.Extensions.TimeEx.Format(timeLeft, false).ToLower()}</color>.");
				}

			});

			action?.Invoke();
			if (doTimer) timer.Every(1f, action);
		}
		public void InitializeNotificationValidator()
		{
			Log($"Initializing notification validator...");

			var action = new Action(() =>
			{
				foreach (var user in Data.Users)
				{
					foreach (var notification in user.Notifications.ToArray())
					{
						try
						{
							if (notification.IsOverdue())
							{
								user.DeleteNotification(notification.Id);
							}
						}
						catch { }
					}
				}
			});

			action?.Invoke();
			timer.Every(ConVar.Server.saveinterval, action);
		}
		public void InitializeDefaultUsers()
		{
			Log($"Initializing default users...");
			Data.GetAdvertsAccount();
			Data.GetLotteryAccount();
		}
		public void InstallDefaultFeeds()
		{
			Log($"Initializing default feeds...");

			var marketplace = Data.GetMarketplaceFeed();
			{
				marketplace.Title = "{marketplace}";
				marketplace.BackgroundUrl = RusterBrowser.MarketplaceBackgroundUrl;
				marketplace.FeedType = RusterFeed.FeedTypes.Shop;
				marketplace.AllowAdverts = false;
			}

			var userItems = Data.GetUserStoreFeed();
			{
				userItems.Title = "Profile Store";
				userItems.BackgroundUrl = RusterBrowser.UserItemsBackgroundUrl;
				userItems.FeedType = RusterFeed.FeedTypes.Shop;
				userItems.AllowAdverts = false;
				userItems.IsLocked = true;
				userItems.ShowDate = false;
				userItems.AllowRatings = false;
				userItems.AllowDeleting = false;
				userItems.DisableFleaMarket = true;
				userItems.ShowHashtags = false;
				userItems.Posts.Clear();

				foreach (var avatar in Config.License.Items.Where(x => x.ItemType == RusterLicensedItem.ItemTypes.Avatar))
				{
					var post = RusterFeed.RusterPost.Create(Data.GetAdvertsAccount(), $"Have a look at this avatar! It might be the right one for you.");
					post.PhotoUrl = avatar.Value;
					post.EmbedPhoto = true;
					post.CanRate = false;
					post.MarketplaceListing = new RusterMarketplaceListing
					{
						Shortname = "photo",
						CustomName = $"{avatar.DisplayName} Avatar",
						Amount = 1,
						AmountLeft = 1,
						WholeStack = true,
						IsPurchased = false,
						Price = avatar.Price
					};
					post.Advert = new RusterFeed.RusterPost.RusterAdvert
					{
						DurationHours = 24
					};
					userItems.Posts.Add(post);
				}

				foreach (var banner in Config.License.Items.Where(x => x.ItemType == RusterLicensedItem.ItemTypes.Frame))
				{
					var post = RusterFeed.RusterPost.Create(Data.GetAdvertsAccount(), $"Have a look at this profile avatar frame! It might be the right one for you.");
					post.PhotoUrl = banner.Value;
					post.EmbedPhoto = true;
					post.CanRate = false;
					post.MarketplaceListing = new RusterMarketplaceListing
					{
						Shortname = "photo",
						CustomName = $"{banner.DisplayName} Frame",
						Amount = 1,
						AmountLeft = 1,
						WholeStack = true,
						IsPurchased = false,
						Price = banner.Price
					};
					post.Advert = new RusterFeed.RusterPost.RusterAdvert
					{
						DurationHours = 24
					};
					userItems.Posts.Add(post);
				}

				foreach (var banner in Config.License.Items.Where(x => x.ItemType == RusterLicensedItem.ItemTypes.Banner))
				{
					var post = RusterFeed.RusterPost.Create(Data.GetAdvertsAccount(), $"Have a look at this profile banner! It might be the right one for you.");
					post.PhotoUrl = banner.Value;
					post.EmbedPhoto = true;
					post.EmbedPhotoRatio = 0.75f;
					post.CanRate = false;
					post.MarketplaceListing = new RusterMarketplaceListing
					{
						Shortname = "photo",
						CustomName = $"{banner.DisplayName} Banner",
						Amount = 1,
						AmountLeft = 1,
						WholeStack = true,
						IsPurchased = false,
						Price = banner.Price
					};
					post.Advert = new RusterFeed.RusterPost.RusterAdvert
					{
						DurationHours = 24
					};
					userItems.Posts.Add(post);
				}
			}

			var lotteryItems = Data.GetLotteryStoreFeed();
			{
				lotteryItems.Title = "Lottery Store";
				lotteryItems.BackgroundUrl = RusterBrowser.UserItemsBackgroundUrl;
				lotteryItems.FeedType = RusterFeed.FeedTypes.Shop;
				lotteryItems.AllowAdverts = false;
				lotteryItems.IsLocked = true;
				lotteryItems.ShowDate = false;
				lotteryItems.AllowRatings = false;
				lotteryItems.AllowDeleting = false;
				lotteryItems.DisableFleaMarket = true;
				lotteryItems.ShowHashtags = false;
				lotteryItems.Posts.Clear();

				lotteryItems.Posts.Add(Data.GetLotteryTicketNoticePost(2, null));
				lotteryItems.Posts.Add(Data.GetLotteryTicketNoticePost(1, null));
				lotteryItems.Posts.Add(Data.GetLotteryTicketNoticePost(0, null));
				lotteryItems.Posts.Add(Data.GetGiftCardNoticePost(50, null));
				lotteryItems.Posts.Add(Data.GetGiftCardNoticePost(100, null));
				lotteryItems.Posts.Add(Data.GetGiftCardNoticePost(250, null));
				lotteryItems.Posts.Add(Data.GetGiftCardNoticePost(500, null));
				lotteryItems.Posts.Add(Data.GetGiftCardNoticePost(0, null));

			}

			var community = Data.GetCommunityFeed();
			{
				community.Title = "{community}";
				community.BackgroundUrl = RusterBrowser.CommunityFeedBackgroundUrl;
				community.FeedType = RusterFeed.FeedTypes.Community;
			}

			var blackmarket = Data.GetFeed(BlackmarketFeedId);
			{
				blackmarket.Title = "Blackmarket";
				blackmarket.BackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/882429000922853416/ruster_blackmarket_1.png";
				blackmarket.FeedType = RusterFeed.FeedTypes.Shop;
				blackmarket.DarkMode = true;

				blackmarket.EnableCensorship = false;
				blackmarket.AllowBlacklistedItems = true;
				blackmarket.AllowAdverts = false;
				blackmarket.AllowPurchases = true;
				blackmarket.AllowRatings = true;
				blackmarket.AllowRestocking = true;
				blackmarket.CanFilter = true;
				blackmarket.ShowHashtags = true;
				blackmarket.ShowLocation = false;
				blackmarket.ShowRatings = false;
				blackmarket.ShowReplies = true;
				blackmarket.WhitelistedListingItems.Clear();
			}

			var redroom = Data.GetFeed(RedRoomFeedId);
			{
				redroom.Title = "Red-Room";
				redroom.BackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/882443794400804926/ruster_redroom.png";
				redroom.FeedType = RusterFeed.FeedTypes.Community;
				redroom.DarkMode = true;

				redroom.EnableCensorship = false;
				redroom.AllowAdverts = true;
				redroom.AllowPurchases = true;
				redroom.AllowRatings = true;
				redroom.AllowRestocking = true;
				redroom.CanFilter = true;
				redroom.ShowHashtags = true;
				redroom.ShowLocation = true;
				redroom.ShowRatings = true;
				redroom.ShowReplies = true;
			}
		}
		public void InstallDefaultGroups()
		{
			Log($"Initializing default groups...");

			Data.GetReportsGroup();
		}
		public void InstallDefaultBots()
		{
			Log($"Initializing default bots...");

			var codeflingBot = Data.GetUser(CodeflingBotId);
			{
				codeflingBot.CurrentDisplayName = "Codefling";
				codeflingBot.AvatarUrl = "https://pbs.twimg.com/profile_images/1345024685541621761/sc75KlbU_400x400.jpg";
				codeflingBot.AllowBusinessCard = false;
				codeflingBot.IsBot = true;
			}

			CodeflingBot.Refresh();
			InstallBot(CodeflingBotId, '!', typeof(CodeflingBot), this);
		}

		#endregion

		#region Localisation

		protected override void LoadDefaultMessages()
		{
			var defaultMessages = new Dictionary<string, string>
			{
				["community"] = "Community",
				["marketplace"] = "Marketplace",
				["useritems"] = "User Items",
				["notifications"] = "Notifications",
				["selfshop"] = "Owned Listings & Adverts",
				["push"] = "Push",
				["rustplus"] = "Rust+",
				["directmessages"] = "Direct Messages",
				["unreadconvos"] = "{0} unread",
				["unreadconvos_pl"] = "{0} unread",

				["giftbasket"] = "Gift Basket",

				["lockedgroup"] = "This group is locked.",
				["blockedcommunication"] = "Blocked communication with <b>{0}</b>",
				["mustbefriends"] = "You must be friends with this person in order to send them direct messages.",
				["sharelocation"] = "Share\nLocation",
				["noconvoyet"] = "You haven't started a conversation with anyone.\nIt's never too late!",
				["teamchat"] = "Team Chat",
				["conversation"] = "Conversation",
				["norecentmessages"] = "No recent messages.",

				["noconvoselected"] = "No conversation selected.\nPlease select one from the left side of the screen to read through.\n<b>←</b>",
				["nomessagesinconvo"] = "Nobody said anything yet. This could be the start of something.\n<b><3</b>",

				["friendrequests"] = "Friend Requests",
				["nofriendrequests"] = "No one on earth that plays on\n<b>{0}</b>\nrequested your friendship.",
				["friends"] = "Friends",
				["nofriends"] = "Aww, shucks!\nYou got no friends.\n<b>:(</b>",
				["blockedtonite"] = "Blocked",
				["sent"] = "Sent",
				["requestsent"] = "Request Sent",
				["acceptrequest"] = "Accept Request",
				["rejectrequest"] = "Reject Request",
				["cancelrequest"] = "Cancel Request",
				["addfriend"] = "Add Friend",
				["removefriend"] = "Remove Friend",

				["accept"] = "Accept",

				["uploadaudio_title"] = "Upload Audio Clip",
				["uploadaudio_c_title"] = "Title",
				["uploadaudio_c_url"] = "URL",
				["uploadaudio_c_skip"] = "Skip",
				["uploadaudio_c_notice"] = "Proceeding to upload a file might take a couple of seconds to minutes. Please use short YouTube clips or small-sized direct-link links.",

				["massrestocknotice"] = "To mass-restock, hold a hammer and\nhit an accessible container with items.\nPress <b><color=orange>[USE]</color></b> to exit.",
				["voicerecordnotice"] = "Select the first slot of the hotbar, then press <b><color=orange>[MOUSE_SECONDARY]</color></b> to start recording your voice memo.\nDeselect the item to end.",

				["feed"] = "Feed",
				["feed_type"] = "{0} Feed",
				["wallet"] = "Wallet",
				["post"] = "Post",
				["posts"] = "Posts",
				["post_count"] = "<b>{0}</b> Post",
				["posts_count"] = "<b>{0}</b> Posts",
				["cantviewfeed"] = "Cannot view this user's feed due to the blocked communication.",
				["nothinghere"] = "Nothing here, so far.",
				["withdraw"] = "WITHDRAW",
				["restock"] = "RESTOCK",
				["transactions"] = "TRANSACTIONS",
				["coupons"] = "COUPONS",
				["reply"] = "Reply",
				["newpost"] = "New Post",
				["doblock"] = "Block",
				["dounblock"] = "Unblock",
				["card"] = "Card",
				["view"] = "View",

				["content"] = "Content",
				["advert"] = "Advert",
				["listing_item"] = "{0} {1} for {2}",
				["free"] = "FREE",
				["instock"] = "In Stock",
				["notinstock"] = "Not In Stock",
				["hour_left"] = "{0} hour left",
				["hours_left"] = "{0} hours left",
				["day_left"] = "{0} day left",
				["days_left"] = "{0} days left",

				["near_place"] = "near {0}",
				["like_count"] = "{0} like",
				["likes_count"] = "{0} likes",
				["dislike_count"] = "{0} dislike",
				["dislikes_count"] = "{0} dislikes",
				["delete"] = "Delete",
				["reply"] = "Reply",
				["replies"] = "Replies",
				["reply_count"] = "<b>{0}</b> reply",
				["replies_count"] = "<b>{0}</b> replies",
				["buy"] = "BUY   ",
				["selected"] = "Selected",
				["notavailable"] = "Not Available",
				["restock_btn"] = "Restock",
				["willpostto"] = "You'll be posting to <b>{0}</b>.",
				["location"] = "Location",
				["addphoto"] = "Add Photo",
				["changephoto"] = "Change Photo",
				["addcassette"] = "Add Cassette",
				["changecassette"] = "Change Cassette",
				["uploadsong"] = "Upload Song",
				["price"] = "Price",
				["selectitem"] = "Select item...",
				["wholestack"] = "Whole Stack",
				["eachitem"] = "Each Item",
				["posttax"] = "There will be a <b>{0}</b> tax.",
				["newposttax"] = "{0} tax per purchase.",
				["read"] = "Read",
				["unread"] = "Unread",
				["poll"] = "Poll",
				["poll_addchoice"] = "Add Choice",
				["poll_duration"] = "Duration:",
				["poll_enterchoice"] = "Enter choice title:",
				["poll_inputempty"] = "The input cannot be empty.",
				["poll_samechoice"] = "Cannot insert the same choice multiple times.",
				["poll_maxchoice_t"] = "Maximum choices reached",
				["poll_maxchoice_c"] = "Remove choices in order to add new ones in their replacement.",
				["poll_enterduration"] = "Enter duration (0.1h - 48h):",
				["fm_stopped"] = "Stopped",
				["fm_openpost"] = "Open Post",
				["fm_viewuser"] = "View User",
				["fm_dmuser"] = "DM User",
				["fm_stop"] = "Stop",
				["fm_nowplaying"] = "Now Playing",

				["nonotifications"] = "No notifications available.",
				["readall"] = "Read All",
				["hide"] = "Hide",
				["show"] = "Show",

				["viewservers"] = "View Servers",
				["serverviewer"] = "Server Viewer",
				["serverheader"] = "{0} servers, {1} blacklisted",
				["rusternetintoffline"] = "<b>Ruster.NET Internet</b> is offline.",
				["noservers"] = "No servers available.",

				["myfeed"] = "My Feed",
				["theirfeed"] = "{0}'s Feed",

				["publish"] = "Publish",
				["cancel"] = "Cancel",
				["close"] = "Close",
				["clear"] = "Clear",
				["delete"] = "Delete",
				["submit"] = "Submit",
				["filter"] = "Filter",

				["search"] = "Search",

				["verifiedacc"] = "Verified Account",
				["verified"] = "Verified",
				["tag_friend"] = "friend",
				["tag_dead"] = "dead",
				["tag_online"] = "online",
				["tag_admin"] = "admin",
				["tag_moderator"] = "moderator",
				["tag_developer"] = "developer",

				["threadislocked"] = "Thread is\nlocked",
				["nostoriesposted"] = "No stories posted by anyone just yet.",

				["contacts"] = "Contacts",
				["contacts_none"] = "No player encounters yet.",
				["filter_none"] = "No user were found with that filter.",

				["dialog_t_deletepost"] = "Are you sure you want to delete this post?",
				["dialog_s_deletepost"] = "This action is irreversible.",
				["dialog_t_removefriend"] = "You're about to remove {0} , are you sure?",
				["dialog_s_removefriend"] = "You're about to delete this user from your list. This action is irreversible, except you re-send them a new Friend Request.",
				["dialog_s_removefriend_1"] = "You'll not be able to direct message them anymore.",
				["dialog_t_block"] = "Are you sure you want to block {0} ?",
				["dialog_s_block"] = "This action is irreversible. You'll permanently block the communication with this user.",
				["dialog_t_locationshare"] = "Are you sure you want to share your location?",
				["dialog_s_locationshare"] = "This might be something you wouldn't wanna do, so we're asking to make sure.",
				["dialog_t_messagedelete"] = "Are you sure you want to delete the message?",
				["dialog_s_messagedelete"] = "This action is irreversible.",
				["dialog_t_convodelete"] = "Are you sure you want to delete the conversation?",
				["dialog_s_convodelete"] = "The other recipient(s) will be notified as the conversation persists in their lists.\nIn order to re-open the conversation, go to their profile and click on <b>DM</b>.",

				["newpostreply"] = "New post reply",

				["notif_t_newfriendreq"] = "New Friend Request",
				["notif_s_newfriendreq"] = "{0} has sent you a friend request.",

				["notif_t_acceptnewfriend"] = "New Friend",
				["notif_s_acceptnewfriend"] = "{0} accepted your friend request.",
				["notif_t_declinenewfriend"] = "New Friend",
				["notif_s_declinenewfriend"] = "{0} declined your friend request.",
				["notif_t_likeblockedcom"] = "Blocked Communication",
				["notif_s_likeblockedcom"] = "You cannot like a post of someone with which you have a blocked communication with.",
				["notif_t_dislikeblockedcom"] = "Blocked Communication",
				["notif_s_dislikeblockedcom"] = "You cannot dislike a post of someone with which you have a blocked communication with.",
				["notif_t_notpublishednocontent"] = "Not Published",
				["notif_s_notpublishednocontent"] = "Your message could have not been sent due to the lack of content you've set.",
				["notif_t_notpublishednoitem"] = "Not Published",
				["notif_s_notpublishednoitem"] = "You must set an item to sell in your listing settings.",
				["notif_t_noimgurclient"] = "No Imgur ClientId",
				["notif_s_noimgurclient"] = "Cannot upload a photo to <b>{0}</b> since the admin hasn't set up Imgur in the config file.",
				["notif_t_bought"] = "{0} bought {1}",
				["notif_s_bought"] = "You've earned {0}",
				["notif_t_purchase"] = "New Purchase",
				["notif_s_purchase"] = "You've successfully purchased this listing.",
				["notif_t_notenoughcurrency"] = "Not Enough {0}",
				["notif_s_notenoughcurrency"] = "You cannot purchase this listing. Make sure you have {0}.",
				["notif_t_reaction"] = "{0} reacted with {1}",
				["notif_t_invalidaudio"] = "Invalid audio setup",
				["notif_s_invalidaudio"] = "Please make sure FFMPEG path is correctly installed in the config.",
				["notif_t_restockfailed"] = "Restocking failed",
				["notif_s_restockblacklisted"] = "Item <b>{0}</b> is blacklisted.",
				["notif_t_liked"] = "{0} liked your {1}",
				["notif_t_disliked"] = "{0} disliked your {1}",
				["notif_t_fullrestock"] = "Fully Restocked",
				["notif_s_fullrestock"] = "You've restocked {0} of your posts using the container items.",
				["notif_t_listingrestock"] = "Listing Restocked",
				["notif_s_listingrestock"] = "Successfully restocked your listing!.",
				["notif_t_itemcondition"] = "Item Condition",
				["notif_s_itemcondition"] = "The item must be in perfect condition to be put up for listing.",
				["notif_t_ssizelistingmismatch"] = "Amount Mismatch",
				["notif_s_ssizelistingmismatch"] = "Make sure the stack-size of the item matches with the listing's stack-size.",
				["notif_t_audiouploademptyurl"] = "Audio URL is empty",
				["notif_s_audiouploademptyurl"] = "Assing a YouTube URL or a direct-link to an audio file.",
				["notif_t_audiouploademptyurl"] = "Audio URL is empty",
				["notif_s_audiouploademptyurl"] = "Assing a YouTube URL or a direct-link to an audio file.",
				["notif_t_maxstories"] = "Excessive Stories",
				["notif_s_maxstories"] = "You've reached the maximum amount of stories.\nWait or remove older ones to upload more.",
				["notif_t_serverlistfail"] = "Server List",
				["notif_s_serverlistfail"] = "Couldn't download server list. The master-server must be offline.",
				["notif_t_serverdatafail"] = "Server Data",
				["notif_s_serverdatafail"] = "Couldn't download server data: {0}",
				["notif_t_friendpost"] = "Friend just posted",
				["notif_s_friendpost"] = "{0} in {1}: <i>{2}</i>",
				["notif_t_mentionpost"] = "You have been mentioned",
				["notif_s_mentionpost"] = "{0} in {1}: <i>{2}</i>",

				["er_postalreadyremoved"] = "This post has already been removed",
				["er_postlikedremoved"] = "This post you're trying to like has been deleted.",
				["er_postdislikedremoved"] = "This post you're trying to dislike has been deleted.",
				["er_postvotedremoved"] = "This post you're trying to vote has been deleted.",
				["er_postreplyremoved"] = "Cannot create a reply to this post since it's been deleted.",
				["er_postpublishremoved"] = "Cannot publish a reply to this post since it's been deleted.",
				["er_postfullyremoved"] = "The post you're willing to fully see has been removed.",
				["er_postpurchaseremoved"] = "You cannot purchase this listing since this post has been removed.",
				["er_postrestockremoved"] = "You cannot restock this listing since this post has been removed.",
				["er_conversationremoved"] = "The conversation has been removed.",
				["er_messagereactremoved"] = "The message you wanted to react to has been removed.",
				["er_poststackremoved"] = "The post has been removed.",
				["er_messagereactblock"] = "You cannot react to a message of someone that blocked you.",

				["er"] = "Emergency Redraw",

				["cov_closing"] = "Closing...",
				["cov_uploadingph"] = "Uploading photo...",
				["cov_accept"] = "Accepting...",
				["cov_photoclear"] = "Photo cleared.",
				["cov_photoupload"] = "Photo successfully uploaded!",
				["cov_photofail"] = "Failed photo upload!",
				["cov_loading"] = "Loading...",
				["cov_uploadingad"] = "Uploading audio...\n<size=10>Go grab some popcorn. This will take a while...</size>",

				["likes_title"] = "Likes",
				["likes_nocontent"] = "No users liked this post.",
				["dislikes_title"] = "Dislikes",
				["dislikes_nocontent"] = "No users disliked this post.",
				["mentions_title"] = "Mentions",
				["mentions_nocontent"] = "No users were mentioned in this post.",

				["24hadv_variation1"] = "Buy an advert which lasts for 24 hours now!",
				["24hadv_variation2"] = "Got something to sell to the community? Get a 24 advert right now!",
				["24hadv_variation3"] = "Buy an advert which lasts for 24 hours now!",

				["1wadv_variation1"] = "Buy an advert which lasts for a week now!",
				["1wadv_variation2"] = "Buy an advert which lasts for a week now!",
				["1wadv_variation3"] = "Buy an advert which lasts for a week now!",

				["time_second"] = "Second",
				["time_seconds"] = "Seconds",
				["time_minute"] = "Minute",
				["time_minutes"] = "Minutes",
				["time_hour"] = "Hour",
				["time_hours"] = "Hours",
				["time_day"] = "Day",
				["time_days"] = "Days",
				["time_week"] = "Week",
				["time_weeks"] = "Weeks",
				["time_postedago"] = "posted {0} ago",
				["time_postedon"] = "posted on {0}"
			};

			foreach (var item in ItemManager.GetItemDefinitions())
			{
				if (string.IsNullOrEmpty(item.displayName.english)) continue;
				defaultMessages.Add($"item_{item.shortname}", item.displayName.english);
			}

			lang.RegisterMessages(defaultMessages, this, lang: "en-GB");
		}

		public static string GetPhrase(string key, ulong? userId = null)
		{
			return Instance.lang.GetMessage(key, Instance, userId?.ToString());
		}

		#endregion

		#region Plugins

		[PluginReference] ImageLibrary ImageLibrary;

		[PluginReference] Plugin GridAPI;

		[PluginReference] Plugin ServerRewards;

		[PluginReference] Plugin Economics;

		[PluginReference] Plugin aMAZEingPro;

		[PluginReference] Plugin Backpacks;

		[PluginReference] Plugin RusterAddons;

		[PluginReference] Plugin RusterNETPro;

		[PluginReference] Plugin WorkshopAPI;

		Plugin OtherPlugin;

		public class Death
		{
			public static string GetImage(string url, bool isPng = true, ulong skin = 0)
			{
				var name = GetStringChecksum(url);

				if (string.IsNullOrEmpty(url)) return string.Empty;

				if ((bool)Instance.ImageLibrary.Call("HasImage", name, skin))
				{
					var success = Instance.ImageLibrary.Call("GetImage", name, skin);
					return !isPng ? null : (string)success;
				}
				else
				{
					Instance.ImageLibrary.Call("AddImage", url, name, skin);
					return !isPng ? url : null;
				}
			}

			public static CuiRawImageComponent GetRawImage(string url, ulong skin = 0, string color = "", float fade = 0.3f, string sprite = "Assets/Icons/rust.png")
			{
				if (ulong.TryParse(url, out skin))
				{
					return GetRawSkinImage(skin, string.Empty, color, fade, sprite);
				}

				var _url = GetImage(url, isPng: false, skin: skin);
				var _png = GetImage(url, skin: skin);

				return new CuiRawImageComponent
				{
					Sprite = sprite,
					Url = _url,
					Png = _png,
					Color = color,
					FadeIn = fade
				};
			}

			public static TimeSince TimeSinceSkinRequest { get; set; }

			public static CuiRawImageComponent GetRawSkinImage(ulong skin = 0, string urlIfFailed = null, string color = "", float fade = 0.3f, string sprite = "Assets/Icons/rust.png")
			{
				var id = skin == 0 ? null : Instance.RequestOrDownloadSkinnedIcon(skin);

				if (string.IsNullOrEmpty(id))
				{
					return GetRawImage(urlIfFailed, skin, color, fade, sprite);
				}
				else
				{
					return new CuiRawImageComponent
					{
						Sprite = sprite,
						Png = id,
						Color = color,
						FadeIn = fade
					};
				}
			}
		}

		public void RefreshPlugins()
		{
			if (ImageLibrary == null || !ImageLibrary.IsLoaded) ImageLibrary = plugins.Find(nameof(ImageLibrary)) as ImageLibrary;
			if (GridAPI == null || !GridAPI.IsLoaded) GridAPI = plugins.Find(nameof(GridAPI));
			if (ServerRewards == null || !ServerRewards.IsLoaded) ServerRewards = plugins.Find(nameof(ServerRewards));
			if (Economics == null || !Economics.IsLoaded) Economics = plugins.Find(nameof(Economics));
			if (aMAZEingPro == null || !aMAZEingPro.IsLoaded) aMAZEingPro = plugins.Find(nameof(aMAZEingPro));
			if (Backpacks == null || !Backpacks.IsLoaded) Backpacks = plugins.Find(nameof(Backpacks));
			if (RusterAddons == null || !RusterAddons.IsLoaded) RusterAddons = plugins.Find(nameof(RusterAddons));
			if (RusterNETPro == null || !RusterNETPro.IsLoaded) RusterNETPro = plugins.Find(nameof(RusterNETPro));
			if (WorkshopAPI == null || !WorkshopAPI.IsLoaded) WorkshopAPI = plugins.Find(nameof(WorkshopAPI));

			if (Config.Currency.CurrencyType == RootConfig.CurrencyConfig.CurrencyTypes.Other)
			{
				OtherPlugin = plugins.Find(Config.Currency.OtherSettings.PluginName);
				OtherPlugin?.Load();
			}

			if (RusterNETPro == null) UnloadPro(); else RefreshPro();
		}

		public string RequestOrDownloadSkinnedIcon(ulong skinId)
		{
			if (WorkshopAPI == null) RefreshPlugins();
			if (WorkshopAPI == null) return null;

			if (!(bool)WorkshopAPI?.Call<bool>("IsGameIconStored", skinId))
			{
				WorkshopAPI?.Call("StoreGameIcon", skinId);
				Death.TimeSinceSkinRequest = 0f;
				return null;
			}

			return WorkshopAPI?.Call("GetGameIcon", skinId)?.ToString();
		}

		#endregion

		#region Permissions

		public const string FactoryPerm = "rusternet.";
		public const string UsePerm = FactoryPerm + "use";
		public const string GetRusterPerm = FactoryPerm + "getruster";
		public const string Get24hAdvertPerm = FactoryPerm + "get24hadvert";
		public const string Get1wAdvertPerm = FactoryPerm + "get1wadvert";
		public const string AdminPerm = FactoryPerm + "admin";
		public const string ModeratorPerm = FactoryPerm + "moderator";
		public const string LaunchPerm = FactoryPerm + "launch";
		public const string VerifiedPerm = FactoryPerm + "verified";
		public const string StoryPerm = FactoryPerm + "story";
		public const string InternetPerm = FactoryPerm + "internet";
		public const string PollPerm = FactoryPerm + "poll";
		public const string TradePerm = FactoryPerm + "trade";

		private void InstallPermissions()
		{
			Log($"Installing permissions...");

			permission.RegisterPermission(UsePerm, this);
			permission.RegisterPermission(AdminPerm, this);
			permission.RegisterPermission(ModeratorPerm, this);
			permission.RegisterPermission(LaunchPerm, this);

			permission.RegisterPermission(GetRusterPerm, this);
			permission.RegisterPermission(Get24hAdvertPerm, this);
			permission.RegisterPermission(Get1wAdvertPerm, this);

			permission.RegisterPermission(VerifiedPerm, this);
			permission.RegisterPermission(StoryPerm, this);
			permission.RegisterPermission(InternetPerm, this);
			permission.RegisterPermission(PollPerm, this);
			permission.RegisterPermission(TradePerm, this);
		}
		private bool HasPermission(BasePlayer player, string perm, bool quiet = false)
		{
			if (!permission.UserHasPermission(player.UserIDString, perm))
			{
				if (!quiet) Print($"You need to have the \"{perm}\" permission.", player);
				return false;
			}

			return true;
		}

		#endregion

		#region Overrides

		public bool IsInitialized { get; set; }
		public bool IsInternetOnline { get; set; }
		public bool IsValid { get; set; }
		public int InternetRetries { get; set; }
		public global::RusterNET.Core.Server[] ServerList { get; set; }
		public global::RusterNET.Core.Server[] ServerBlacklist { get; set; }
		public MonumentInfo[] Monuments { get; set; }

		public const string InternetServer = @"http://167.86.121.152:8802/rusternet-ms";
		public const string InternetServerList = @"http://167.86.121.152:8802/rusternet-ms?method=serverlist";
		public const string InternetServerBlacklist = @"http://167.86.121.152:8802/rusternet-ms?method=serverblacklist";
		public const string InternetServerInfo = @"http://167.86.121.152:8802/rusternet-ms?method=serverinfo";

		private void Init()
		{
			Instance = this;

			GetVanillaStackSizes();
			GetFleaMarket();
		}
		private void Loaded()
		{
			if (!IsInitialized) return;

			Monuments = TerrainMeta.Path.Monuments.ToArray();

			if (ConfigFile == null) ConfigFile = new Core.Configuration.DynamicConfigFile($"{Manager.ConfigPath}{Path.DirectorySeparatorChar}{Name}.json");
			if (DataFile == null) DataFile = Interface.Oxide.DataFileSystem.GetFile($"{Name}_data");

			if (!ConfigFile.Exists()) ConfigFile.WriteObject(Config = new RootConfig());
			else
			{
				try
				{
					Config = ConfigFile.ReadObject<RootConfig>();
					RefreshPlugins();
				}
				catch (Exception exception) { Puts($"Broken configuration: {exception.Message}"); }

				if (string.IsNullOrEmpty(Config.Sounds.FFMPEGPath)) Config.Sounds.FFMPEGPath = $"{GetTempFolder()}{Path.DirectorySeparatorChar}ffmpeg.exe";
			}

			switch (Config.DataType)
			{
				case RootConfig.DataTypes.JSON:
					if (DataFile.Exists()) Data = DataFile.ReadObject<RootData>();
					else Data = new RootData();
					break;

				case RootConfig.DataTypes.SQL:
					InitializeSQL();
					LoadSQL();
					break;
			}

			foreach (var player in BasePlayer.activePlayerList)
			{
				Data.UpdateItems(player.inventory.AllItems(), Data.GetUser(player));
			}

			Data.Fixups();

			InitializeDefaultUsers();
			InitializeAdvertValidator();
			InitializeStoryValidator();
			InitializeNotificationValidator();
			InitializeLotteryValidator();
			InstallDefaultFeeds();
			InstallDefaultGroups();
			InstallDefaultBots();
			InstallEventCommands();
			InstallUserCommands();
			InstallPermissions();
			GetRusterWatermarkPath();
			GetRusterMarketplaceWatermarkPath();

			if (Config.Internet.Enable)
			{
				var firstTrigger = false;

				timer.Every(10f, () =>
				{
					var previousState = IsInternetOnline;

					webrequest.Enqueue(InternetServer, string.Empty, (int code, string data) =>
					{
						if (code != 200 && InternetRetries < 3)
						{
							InternetRetries++;
							return;
						}

						InternetRetries = 0;
						IsInternetOnline = code == 200;

						if (previousState != IsInternetOnline || !firstTrigger)
						{
							if (IsInternetOnline)
							{
								Log($"Ruster.NET cross-server support is enabled.");
								global::RusterNET.Core.Internet.RusterValidate((bool isValid) => { IsValid = isValid; });
							}
							else
							{
								Log($"Ruster.NET cross-server support is disabled due to the master-server being offline.");
							}

							firstTrigger = true;
						}

						previousState = IsInternetOnline;
					}, this);
				});

				timer.Every(15f, () =>
				{
					if (!IsInternetOnline || !IsValid) return;

					global::RusterNET.Core.Internet.ServerTick();
				});

				timer.Every(60f, () =>
				{
					if (!IsInternetOnline || !IsValid) return;

					global::RusterNET.Core.Internet.ServerFetch(Data, Config);
				});

				var content = timer.Every(60f * 1.5f, () =>
				{
					if (!IsInternetOnline || !IsValid) return;

					global::RusterNET.Core.Internet.RusterFetch(Data);
				});
				timer.In(40f, content.Callback);
			}

			if (Config.Localisation.AutoUpdatePhrases)
			{
				Config.Localisation.UpdatePhrases(false);
			}
		}
		private void Unload()
		{
#if !CARBON
			webrequest.Shutdown();
#endif

			foreach (var player in BasePlayer.activePlayerList)
			{
				if (!Browsers.ContainsKey(player.userID)) continue;

				var browser = Browsers[player.userID];

				if (browser.SellingContainer != null) foreach (var item in browser.SellingContainer.itemList.ToArray()) { player.GiveItem(item); }
				else if (browser.MarketplaceItem != null && !string.IsNullOrEmpty(browser.MarketplaceItem.Shortname))
				{
					var item = browser.MarketplaceItem.CreateItem();
					if (item != null) player.GiveItem(item);
				}
				if (browser.PhotographContainer != null) foreach (var item in browser.PhotographContainer.itemList.ToArray()) { player.GiveItem(item); }

				browser.Refund();
				browser.CloseFully();
				browser.CloseProfile();
				browser.CloseNotice();
				browser.CloseFullPosts();
				browser.RestoreHeldItem();

				browser.SellingContainer?.Kill();
				browser.SellingContainer = null;
			}

			UninitializeSQL();
		}
		private void OnPluginLoaded(Plugin name)
		{
			RefreshPlugins();
		}
		private void OnPluginUnloaded(Plugin name)
		{
			RefreshPlugins();
			DoBotRemovesFor(name);
		}
		private void OnServerInitialized()
		{
			Instance = this;
			IsInitialized = true;

			global::RusterNET.Core.Internet.Version = $"Ruster.NET_v{Version.ToString()}";
			global::RusterNET.Core.Internet.Path = Path.Combine(Path.Combine(Interface.Oxide.PluginDirectory, "RusterNET.cs"));

			webrequest = new WebRequests();

			LatestVersion = Version.ToString();
			FetchLatestVersion((version) => { LatestVersion = version; });

			timer.Every(60f, () => { FetchLatestVersion((version) => { LatestVersion = version; }); });
			timer.Every(60f * 30, () => { CodeflingBot.Refresh(); });

			Loaded();
		}
		private void OnServerSave()
		{
			if (!IsInitialized) return;

			if (Config != null) ConfigFile.WriteObject(Config);

			InitializeStoryValidator(false);
			InitializeAdvertValidator(false);

			switch (Config.DataType)
			{
				case RootConfig.DataTypes.JSON:
					if (Data != null) DataFile.WriteObject(Data);
					break;

				case RootConfig.DataTypes.SQL:
					SaveSQL();
					break;
			}
		}
		private object OnUserCommand(IPlayer iPlayer, string command, string[] args)
		{
			if (command.ToLower().Contains($"ruster")) return null;

			var player = iPlayer.Object as BasePlayer;
			var browser = GetBrowser(player);
			if (browser.IsOpen)
			{
				return true;
			}

			return null;
		}
		private object OnServerCommand(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null) return null;

			if (arg.cmd.FullName.Contains($"ruster") || arg.cmd.FullName == "closeruster") return null;

			var browser = GetBrowser(player);
			if (browser.IsOpen)
			{
				return true;
			}

			return null;
		}
		private void OnPlayerConnected(BasePlayer player)
		{
			var user = Data.GetUser(player);
			user.Refresh();
		}
		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return;

			var user = Data.GetUser(player);
			user.Refresh();

			var browser = GetBrowser(player);
			browser.Close();
		}
		private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
		{
			if (!HasPermission(player, UsePerm) || !player.userID.IsSteamId() || player.IsNpc) return;

			var browser = GetBrowser(player);

			if (browser.PanelType == RusterBrowser.PanelTypes.NewPost && browser.HeldItemContainer == null) return;

			if (oldItem != null && (
				oldItem.skin == RusterShortFlipbookSkinId ||
				oldItem.skin == RusterMediumFlipbookSkinId ||
				oldItem.skin == RusterLongFlipbookSkinId))
			{
				browser.CloseScreenNotice();
			}

			if (newItem != null)
			{
				if (newItem.skin == Instance.GetProConfig(nameof(RusterSkinId), RusterSkinId))
				{
					if (!browser.PendingSlotConfirmation) { browser.PendingSlotConfirmation = true; browser.DrawLaunchConfirmation(); return; }
					Launch(player);
				}
				else if (newItem.skin == Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinId), RusterMarketplace24hAdvertSkinId))
				{
					if (!browser.PendingSlotConfirmation) { browser.PendingSlotConfirmation = true; browser.DrawLaunchConfirmation(); return; }
					if (!browser.IsOpen)
					{
						browser.PanelType = RusterBrowser.PanelTypes.NewPost;
						browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
						browser.FeedId = MarketplaceFeedId;
						browser.MarketplaceItem = new RusterMarketplaceListing() { WholeStack = Config.Marketplace.ValidateWholeStack(false) };
						browser.IsAdvertPost = true;
						browser.Is24hAdvert = true;
						Launch(player);
					}
				}
				else if (newItem.skin == Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinId), RusterMarketplace1wAdvertSkinId))
				{
					if (!browser.PendingSlotConfirmation) { browser.PendingSlotConfirmation = true; return; }
					if (!browser.IsOpen)
					{
						browser.PanelType = RusterBrowser.PanelTypes.NewPost;
						browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
						browser.FeedId = MarketplaceFeedId;
						browser.MarketplaceItem = new RusterMarketplaceListing() { WholeStack = Config.Marketplace.ValidateWholeStack(false) };
						browser.IsAdvertPost = true;
						browser.Is24hAdvert = false;
						Launch(player);
					}
				}
				else if (newItem.skin == Instance.GetProConfig(nameof(RusterBusinessCardSkinId), RusterBusinessCardSkinId))
				{
					if (string.IsNullOrEmpty(newItem.text)) return;

					if (!browser.PendingSlotConfirmation) { browser.PendingSlotConfirmation = true; browser.DrawLaunchConfirmation(); return; }
					var card = JsonConvert.DeserializeObject<RusterBusinessCard>(newItem.text);
					if (!browser.IsOpen)
					{
						var user = card.GetUser();
						browser.PanelType = RusterBrowser.PanelTypes.None;
						browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
						browser.CurrentUserId = user.Id;
						Launch(player);
					}
				}
				else if (newItem.skin == RusterShortFlipbookSkinId || newItem.skin == RusterMediumFlipbookSkinId || newItem.skin == RusterLongFlipbookSkinId)
				{
					var flipbook = JsonConvert.DeserializeObject<RusterFlipbook>(newItem.text);

					if (string.IsNullOrEmpty(Config.PhotographUpload.ImgurClientId))
					{
						browser.DrawScreenNotice("<size=13><b>Imgur Client ID is not set!</b></size>\nIn order to use <color=orange><b>Flipbook</b></color>, please configure the client ID for Imgur uploading.");
					}
					else if (string.IsNullOrEmpty(flipbook.ThumbnailUrl))
					{
						browser.LaunchConfirmationTimer?.Destroy();
						browser.DrawScreenNotice("<size=13>This is the thumbnail picture\nof this <color=orange><b>Flipbook</b></color>.</size>");
					}
					else
					{
						browser.DrawFlipbookNotice(flipbook);
					}

					browser.PendingSlotConfirmation = false;
					browser.LaunchConfirmationTimer?.Destroy();
				}
				else if (newItem.skin == RusterGiftCardSkinId)
				{
					var giftCard = JsonConvert.DeserializeObject<RusterGiftCard>(newItem.text);

					if (!browser.PendingSlotConfirmation) { browser.PendingSlotConfirmation = true; browser.DrawLaunchConfirmation(); return; }
					if (!browser.IsOpen)
					{
						browser.DrawConfirmDialog(
							"Redeem Gift Card",
							$"Are you sure you wanna redeem this <color=green>{Instance.Config.Currency.GetValueName(browser, giftCard.Value, true)}</color> Gift Card?\nThis action is irreversible.",
							onAccept: () =>
							{
								browser.User.Wallet += giftCard.Value;
								newItem.Remove(.1f);

								browser.DrawCover("giftcardlol", $"Successfully redeemed <color=green>{Instance.Config.Currency.GetValueName(browser, giftCard.Value, true)}</color> Gift Card!", closeAfter: 3f);
							});
					}
				}
				else
				{
					if (browser.PendingSlotConfirmation) browser.CloseScreenNotice();
					browser.PendingSlotConfirmation = false;
				}
			}
			else
			{
				if (browser.HeldItemContainer != null)
				{
					var cassetteItem = browser.TemporaryHeldItem.contents.itemList[0];
					var cassette = BaseNetworkable.serverEntities.Find(cassetteItem.instanceData.subEntity) as Cassette;

					if (cassette.AudioId != 0)
					{
						browser.UploadedCassetteId = CopyData(cassetteItem, player.userID) ?? 0;
						if (browser.UploadedCassetteId != 0)
							browser.UploadedCassetteTitle = $"{player.displayName}'s Memo";
					}

					browser.CloseScreenNotice();
					browser.RestoreHeldItem();
					browser.Draw(onDraw: browser.DrawOverlays);
				}
			}
		}
		private void OnItemDropped(Item item, BaseEntity entity)
		{
			try
			{
				var player = item.parent?.playerOwner;

				if (item == null || item.parent == null || item.parent.playerOwner == null)
				{
					var throwingPlayers = Facepunch.Pool.GetList<BasePlayer>();
					Vis.Entities(entity.transform.position, 0.2f, throwingPlayers);

					player = throwingPlayers.FirstOrDefault();

					Facepunch.Pool.FreeList(ref throwingPlayers);
				}

				var browser = GetBrowser(player);
				var activeItem = browser.Player.GetActiveItem();

				if (item.info.shortname == "photo" && (
					activeItem.skin == RusterLongFlipbookSkinId ||
					activeItem.skin == RusterMediumFlipbookSkinId ||
					activeItem.skin == RusterShortFlipbookSkinId) &&
					 player.inventory.containerBelt.IsFull() &&
					player.inventory.containerMain.IsFull())
				{
					NextTick(() =>
					{
						entity.Kill();

						var flipbook = JsonConvert.DeserializeObject<RusterFlipbook>(activeItem.text);
						activeItem.condition = ((float)flipbook.Frames).Scale(0f, flipbook.MaximumFrames + 1, activeItem.maxCondition, 0f);

						browser.DrawCover("fullinv", "Your inventory is full. Please have at least one slot available.", closeAfter: 2f);
					});
				}
				else if (activeItem.skin == RusterShortFlipbookSkinId ||
				  activeItem.skin == RusterMediumFlipbookSkinId ||
				  activeItem.skin == RusterLongFlipbookSkinId)
				{
					browser.CloseScreenNotice();
				}
			}
			catch { }
		}
		private void OnPlayerLootEnd(PlayerLoot playerLoot)
		{
			if (playerLoot == null) return;

			var player = playerLoot.baseEntity;
			if (player == null || !player.userID.IsSteamId() || player.IsNpc) return;

			var browser = GetBrowser(player);
			var exception = (Exception)null;

			var isConversationMessage = browser.PanelType == RusterBrowser.PanelTypes.DirectMessages;

			if (browser.UploadingPhotograph && browser.PhotographContainer != null)
			{
				var customNoticeText = string.Empty;

				if (browser.Poll != null && browser.PhotographContainer.itemList.Count != 0)
				{
					var photo = browser.PhotographContainer.itemList[0];
					player.GiveItem(photo);

					browser.UploadedPhotograph = browser.UploadedPhotographTag = null;
				}
				else if (browser.PhotographContainer.itemList.Count != 0)
				{
					var item = browser.PhotographContainer.itemList[0];
					if (item.info.shortname == "photo")
					{
						browser.DrawCover("photo", browser.GetPhrase("cov_uploadingph"), false);

						var photoEntities = BaseNetworkable.serverEntities.OfType<PhotoEntity>();
						var photoEntity = photoEntities.FirstOrDefault(x => x.net.ID == item.instanceData.subEntity);
						var photoData = FileStorage.server.Get(photoEntity.ImageCrc, FileStorage.Type.jpg, photoEntity.net.ID);
						var folder = GetTempFolder();
						var file = $"{folder}{Path.DirectorySeparatorChar}{photoEntity.ImageCrc}_{photoEntity.net.ID}_{DateTime.Now.Ticks}.jpg";
						var temporaryFile = $"{folder}{Path.DirectorySeparatorChar}{photoEntity.ImageCrc}_{photoEntity.net.ID}_{DateTime.Now.Ticks}_temp.jpg";

						OsEx.File.Create(file, photoData);
						var isMarketplaceListing = browser.MarketplaceItem != null;
						var imgurUrl = isConversationMessage ? global::RusterNET.Core.PhotoUpload.UploadImageToImgur(file, Config.PhotographUpload.ImgurClientId, out exception) : global::RusterNET.Core.PhotoUpload.UploadImageToImgurWithWatermark(file, temporaryFile, Config.PhotographUpload.ImgurClientId,
							isMarketplaceListing ? GetRusterMarketplaceWatermarkPath() : GetRusterWatermarkPath(), out exception,
							offsetY: isMarketplaceListing ? 1.14f : 0,
							height: isMarketplaceListing ? 50 : 0);
						browser.UploadedPhotograph = imgurUrl;
						browser.UploadedPhotographTag = item.text;

						if (exception != null)
						{
							browser.CloseCover("photo");
							Puts(exception.ToString());
						}

						ServerMgr.Instance.Invoke(() =>
						{
							try
							{
								OsEx.File.Delete(file);
								OsEx.File.Delete(temporaryFile);
							}
							catch { }
						}, 3f);

						player.GiveItem(item);
						browser.CloseCover("photo");

						if (isConversationMessage)
						{
							var conversation = Data.GetConversation(browser.ConversationId);
							var message = new RusterConversation.RusterDirectMessage(conversation, browser.User.Id, string.Empty, true) { PhotographUrl = imgurUrl };
							conversation.PostMessage(message);

							browser.UploadedPhotograph = browser.UploadedPhotographTag = null;
						}
					}
					else if (item.skin == RusterShortFlipbookSkinId ||
						item.skin == RusterMediumFlipbookSkinId ||
						item.skin == RusterLongFlipbookSkinId)
					{
						if (!isConversationMessage)
						{
							browser.DrawCover("photo", "Processing Flipbook...", closeAfter: 2f);

							var flipbook = JsonConvert.DeserializeObject<RusterFlipbook>(item.text);
							if (string.IsNullOrEmpty(flipbook.ThumbnailUrl))
							{
								customNoticeText = $"This Flipbook is empty.\nYou need at least {Config.PhotographUpload.MinimumFlipbookFrames:n0} frames in this Flipbook to upload.";
							}
							else if (flipbook.Frames >= Config.PhotographUpload.MinimumFlipbookFrames)
							{
								var id = GetStringChecksum(flipbook.Id);

								browser.PostGif = new RusterFeed.RusterPost.RusterGif()
								{
									Id = flipbook.Id,
									Frames = flipbook.Frames,
									IsFlipbook = true
								};
								browser.UploadedPhotograph = flipbook.ThumbnailUrl;
								browser.CloseCover("photo");
								customNoticeText = $"Flipbook successfully uploaded!";
							}
							else
							{
								customNoticeText = $"You need at least {Config.PhotographUpload.MinimumFlipbookFrames:n0} frames in this Flipbook to upload.";
							}
						}
						else
						{
							customNoticeText = $"You cannot send Flipbooks in direct messages.";
						}

						player.GiveItem(item);
						browser.CloseCover("photo");
					}
				}
				else
				{
					browser.UploadedPhotograph = browser.UploadedPhotographTag = null;
				}

				browser.UploadingPhotograph = false;
				browser.PhotographContainer?.Kill();
				browser.PhotographContainer = null;
				browser.Draw(onDraw: () => { browser.DrawCover("photo_confirm", !string.IsNullOrEmpty(customNoticeText) ? customNoticeText : exception == null ? string.IsNullOrEmpty(browser.UploadedPhotograph) ? browser.GetPhrase("cov_photoclear") : browser.GetPhrase("cov_photoupload") : browser.GetPhrase("cov_photofail"), closeAfter: 2f); });
			}
			else if (browser.UploadingStory)
			{
				if (browser.PhotographContainer != null && browser.PhotographContainer.itemList.Count != 0)
				{
					browser.DrawCover("photo", browser.GetPhrase("cov_uploadingph"), false);
					browser.UploadingStory = false;

					var photo = browser.PhotographContainer.itemList[0];
					var story = Instance.Data.CreateStory(browser.User, photo);
					player.GiveItem(photo);

					browser.CloseCover("photo");

					if (story != null)
					{
						browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
						browser.Draw();
					}
				}
				else
				{
					browser.UploadingStory = false;
					browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
					browser.Draw();
				}
			}
			else if (browser.MarketplaceItem != null && browser.SellingContainer != null)
			{
				if (browser.SellingContainer.itemList.Count != 0)
				{
					var item = browser.SellingContainer.itemList[0];
					var feed = Data.GetFeed(browser.FeedId);

					var isBlacklisted = true;
					if (feed.BlacklistedListingItems.Count != 0 && !feed.BlacklistedListingItems.Any(x => x == item.info.shortname || x == item.info.itemid.ToString())) isBlacklisted = false;
					else if (feed.WhitelistedListingItems.Count > 0)
					{
						if (feed.WhitelistedListingItems.Any(x => x == item.info.shortname || x == item.info.itemid.ToString())) isBlacklisted = false;
					}
					else if (!Config.Marketplace.BlacklistedItems.Any(x => x == item.info.shortname || x == item.info.itemid.ToString()) || feed.AllowBlacklistedItems) isBlacklisted = false;

					if (isBlacklisted)
					{
						player?.GiveItem(item);
						ServerMgr.Instance.Invoke(() => browser.Notify(browser.GetPhrase("notif_t_restockfailed"), browser.GetPhrase("notif_s_restockblacklisted", GetPhrase(item.info.displayName.english)), playSound: false), 0.2f);
					}
					else if (item.condition != item.maxCondition)
					{
						player?.GiveItem(item);
						ServerMgr.Instance.Invoke(() => browser.Notify(browser.GetPhrase("notif_t_itemcondition"), browser.GetPhrase("notif_s_itemcondition"), playSound: false), 0.2f);
					}
					else if (browser.RestockedPostId != null)
					{
						var post = Instance.Data.GetPost(browser.RestockedPostId.Value);
						if (post != null)
						{
							if (post.MarketplaceListing.Shortname == item.info.shortname)
							{
								if (!post.MarketplaceListing.WholeStack)
								{
									post.MarketplaceListing.AmountLeft += item.amount;
									post.MarketplaceListing.AmountLeft = post.MarketplaceListing.AmountLeft.Clamp(0, post.MarketplaceListing.Amount);
								}
								post.MarketplaceListing.IsPurchased = false;
								post.MarketplaceListing.Text = item.text;
								if (item.instanceData != null) post.MarketplaceListing.SubEntityId = CopyData(item, browser.User.Id) ?? 0;
								ServerMgr.Instance.Invoke(() => browser.Notify(browser.GetPhrase("notif_t_listingrestock"), browser.GetPhrase("notif_s_listingrestock")), 0.2f);
								Instance.RusterAddons?.Call("RNETAPI_OnRestock", player.userID, new RusterFeed.RusterPost[] { post });
							}
							else
							{
								player?.GiveItem(item);
								ServerMgr.Instance.Invoke(() => browser.Notify(browser.GetPhrase("notif_t_ssizelistingmismatch"), browser.GetPhrase("notif_s_ssizelistingmismatch"), playSound: false), 0.2f);
							}

							browser.RestockedPostId = null;
							browser.Clear();
						}
					}
					else
					{
						var existentItem = browser.MarketplaceItem.CreateItem();
						if (existentItem != null) player?.GiveItem(existentItem);

						browser.MarketplaceItem.SetSoldItem(item, item.instanceData == null ? 0 : CopyData(item, browser.User.Id));
					}
				}
				else { browser.MarketplaceItem._Item = null; }

				browser.Draw(onDraw: browser.DrawOverlays);
			}
			else if (browser.CassetteContainer != null)
			{
				if (browser.CassetteContainer.itemList.Count != 0)
				{
					var cassette = browser.CassetteContainer.itemList[0];
					var shortName = cassette.info.shortname;

					if (shortName == "cassette" || shortName == "cassette.medium" || shortName == "cassette.short")
					{
						browser.UploadedCassetteId = CopyData(cassette, browser.User.Id) ?? 0;
						browser.UploadedCassetteTitle = cassette.text;

						if (isConversationMessage)
						{
							var conversation = Data.GetConversation(browser.ConversationId);
							var message = new RusterConversation.RusterDirectMessage(conversation, browser.User.Id, string.Empty, true) { CassetteId = browser.UploadedCassetteId };
							conversation.PostMessage(message);

							browser.UploadedCassetteId = 0;
							browser.UploadedCassetteTitle = null;
						}
					}
					else
					{
						browser.UploadedCassetteId = 0;
						browser.UploadedCassetteTitle = null;
					}

					player.GiveItem(cassette);
				}
				else
				{
					browser.UploadedCassetteId = 0;
					browser.UploadedCassetteTitle = null;
				}

				browser.CassetteContainer?.Kill();
				browser.CassetteContainer = null;
				browser.Draw(onDraw: () => { browser.DrawCover("cassette_confirm", browser.UploadedCassetteId == 0 ? "Cassette cleared." : "Cassette has been set.", closeAfter: 1f); });
			}
			else if (browser.GiftBasketContainer != null)
			{
				var basket = Data.GetGiftBasket(browser.User);
				basket.Clear();
				basket.AddRange(browser.GiftBasketContainer.itemList.Select(x => new RusterMarketplaceListing
				{
					CustomName = x.name,
					Shortname = x.info.shortname,
					Skin = x.skin,
					Amount = x.amount,
					Text = x.text,
					SubEntityId = x.instanceData == null ? 0 : x.instanceData.subEntity.Value,
					WholeStack = true
				}));

				browser.GiftBasketContainer?.Kill();
				browser.GiftBasketContainer = null;
				browser.Draw(onDraw: () => { browser.DrawCover("giftbasket", "Saved Gift Basket...", closeAfter: 1f); });
			}
		}
		public ulong? CopyData(Item item, ulong playerId)
		{
			var customEntityData = (ulong?)null;
			if (item?.instanceData?.subEntity.Value != 0)
			{
				var entity = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity);
				if (entity != null && entity is PhotoEntity)
				{
					var photo = entity as PhotoEntity;
					var photoCopy = GameManager.server.CreateEntity(entity.PrefabName, entity.transform.position, entity.transform.rotation) as PhotoEntity;
					var data = FileStorage.server.Get(photo.ImageCrc, FileStorage.Type.jpg, photo.net.ID);
					photoCopy.Spawn();

					photoCopy.SetImageData(playerId, data);

					customEntityData = photoCopy.net.ID.Value;
				}
				else if (entity != null && entity is Cassette)
				{
					var cassette = entity as Cassette;
					var cassetteCopy = GameManager.server.CreateEntity(entity.PrefabName, entity.transform.position, entity.transform.rotation) as Cassette;
					var data = FileStorage.server.Get(cassette.AudioId, FileStorage.Type.ogg, cassette.net.ID);
					cassetteCopy.Spawn();

					if (data != null && data.Length != 0)
					{
						var dataCopy = FileStorage.server.Store(data, FileStorage.Type.ogg, cassetteCopy.net.ID);

						cassetteCopy.SetAudioId(dataCopy, playerId);
					}

					customEntityData = cassetteCopy.net.ID.Value;
				}
			}
			return customEntityData;
		}
		private object CanLootPlayer(BasePlayer looted, BasePlayer looter)
		{
			if (!looter.userID.IsSteamId() || looter.IsNpc) return null;

			var browser = GetBrowser(looter);

			if (browser.UploadingPhotograph ||
				browser.UploadingStory ||
				browser.MarketplaceItem != null ||
				browser.CassetteContainer != null ||
				browser.GiftBasketContainer != null ||
				browser.Trade != null)
			{
				return true;
			}

			return null;
		}
		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if (container == null || item == null || container.playerOwner == null)
				return;

			var activeItem = container.playerOwner.GetActiveItem();
			if (activeItem != null && (activeItem.skin == RusterShortFlipbookSkinId || activeItem.skin == RusterMediumFlipbookSkinId || activeItem.skin == RusterLongFlipbookSkinId) && item.info.shortname == "photo")
			{
				var flipbook = JsonConvert.DeserializeObject<RusterFlipbook>(activeItem.text);
				if (flipbook != null)
				{
					var browser = GetBrowser(container.playerOwner);
					var photo = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as PhotoEntity;
					var data = FileStorage.server.Get(photo.ImageCrc, FileStorage.Type.jpg, photo.net.ID);

					if (string.IsNullOrEmpty(flipbook.ThumbnailUrl))
					{
						browser.DrawCover("thumbnice", "Took the thumbnail picture for this Flipbook!", closeAfter: 2f);

						var thumbnailPath = $"{GetTempFolder()}{Path.DirectorySeparatorChar}thumbnail_{flipbook.Id}.jpg";
						OsEx.File.Create(thumbnailPath, data);

						var exception = (Exception)null;
						flipbook.ThumbnailUrl = global::RusterNET.Core.PhotoUpload.UploadImageToImgur(thumbnailPath, Instance.Config.PhotographUpload.ImgurClientId, out exception);

						if (exception == null)
						{
							Instance.timer.In(1f, () => { try { OsEx.File.Delete(thumbnailPath); } catch { } });
							activeItem.condition = ((float)flipbook.Frames).Scale(0f, flipbook.MaximumFrames + 1, activeItem.maxCondition, 0f);
						}
						else
						{
							browser.DrawCover("thumbnice", $"Failed uploading the thumbnail for this Flipbook!\n<size=9>{exception.Message}</size>", closeAfter: 3.5f);
						}
					}
					else if (flipbook.Frames < flipbook.MaximumFrames)
					{
						ImageLibrary.AddImageData($"{flipbook.Id}_{flipbook.Frames}", data, 0);

						flipbook.Frames++;
						activeItem.condition = ((float)flipbook.Frames).Scale(0f, flipbook.MaximumFrames + 1, activeItem.maxCondition, 0f);
						activeItem.SetFlag(global::Item.Flag.OnFire, true);
						timer.In(3f, () =>
						{
							activeItem.SetFlag(global::Item.Flag.OnFire, false);
							activeItem.MarkDirty();
						});

						if (flipbook.Frames == flipbook.MaximumFrames) activeItem.condition = 0;
					}
					else
					{
						activeItem.condition = 0;
					}

					activeItem.text = JsonConvert.SerializeObject(flipbook);

					item.Remove(.1f);
					browser.DrawFlipbookNotice(flipbook);
				}
			}

			Data.UpdateItem(item);
		}
		private object OnPlayerDeath(BasePlayer player, HitInfo info)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			var browser = GetBrowser(player);

			try
			{
				browser.Refund();
				browser.CloseFully();
			}
			catch { }

			return null;
		}
		private object OnPlayerSleep(BasePlayer player)
		{
			if (player == null || !player.userID.IsSteamId() || player.IsNpc) return null;

			var browser = GetBrowser(player);

			try
			{
				browser.Refund();
				browser.CloseFully();
			}
			catch { }

			return null;
		}
		private object OnPlayerWound(BasePlayer player, HitInfo hitInfo)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			var browser = GetBrowser(player);
			browser.Refund();
			browser.Close();

			return null;
		}
		private object OnHammerHit(BasePlayer player, HitInfo info)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			var browser = GetBrowser(player);
			var storage = info.HitEntity as StorageContainer;
			if (storage == null) return null;

			var @lock = storage.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
			if (@lock != null && @lock.IsLocked()) return null;

			if (browser.FullRestockMode)
			{
				var restockedPosts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				var user = Data.GetUser(player);
				foreach (var post in user.GetStorePosts())
				{
					var listing = post.MarketplaceListing;
					if (!listing.IsPurchased) continue;

					if (browser.TakeStorageItems(listing.Shortname, listing.Amount, storage.inventory, mustBeMaximumCondition: true))
					{
						listing.IsPurchased = false;
						listing.AmountLeft = listing.Amount;
						listing.AmountLeft = listing.AmountLeft.Clamp(0, listing.Amount);

						restockedPosts.Add(post);
					}
				}

				if (restockedPosts.Count > 0)
				{
					Instance.RusterAddons?.Call("RNETAPI_OnRestock", user.Id, restockedPosts.ToArray());
				}

				browser.FullRestockMode = false;
				browser.Draw();
				browser.DrawOverlays();

				if (restockedPosts.Count > 0) browser.Notify(browser.GetPhrase("notif_t_fullrestock"), browser.GetPhrase("notif_s_fullrestock", restockedPosts.Count.ToString("n0")));
				Facepunch.Pool.FreeList(ref restockedPosts);
				return false;
			}

			return null;
		}
		private void OnPlayerInput(BasePlayer player, InputState input)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return;

			if (input.WasJustPressed(BUTTON.USE))
			{
				var browser = GetBrowser(player);
				if (browser.FullRestockMode)
				{
					browser.FullRestockMode = false;
					browser.Draw(onDraw: browser.DrawOverlays);
				}
				if (browser.GifProcessor != null && browser.GifProcessor.IsRendering)
				{
					browser.GifProcessor.StopRender();
				}
			}

			if (player.mounted.IsValid(true))
			{
				var browser = GetBrowser(player);
				if (browser.IsOpen)
				{
					input.Clear();
				}
			}
		}
		private object OnInventoryNetworkUpdate(PlayerInventory inventory, ItemContainer container, ProtoBuf.UpdateItemContainer updateItemContainer, PlayerInventory.Type type, bool broadcast)
		{
			var player = inventory.baseEntity;
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			Data.UpdateItems(inventory.AllItems(), Data.GetUser(player));

			return null;
		}
		private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
		{
			if (targetItem.item.skin == RusterMarketplace1wAdvertSkinId ||
				targetItem.item.skin == RusterMarketplace24hAdvertSkinId ||
				targetItem.item.skin == RusterBusinessCardSkinId)
				if (targetItem.item.skin == item.item.skin && targetItem.item.text != item.item.text) return true;
				else if (targetItem.item.skin != item.item.skin) return true;

			return null;
		}
		private object CanStackItem(Item item, Item targetItem)
		{
			if ((targetItem.skin == RusterMarketplace1wAdvertSkinId ||
				targetItem.skin == RusterMarketplace24hAdvertSkinId ||
				targetItem.skin == RusterBusinessCardSkinId) && targetItem.skin == item.skin && targetItem.text != item.text ||
				targetItem.skin == RusterGiftCardSkinId)
				return false;

			return null;
		}
		private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
		{
			if (entity == null || entity.IsDestroyed || !player.userID.IsSteamId() || player.IsNpc) return;

			var browser = GetBrowser(player);
			if (browser.IsOpen)
			{
				entity.MountPlayer(player);
			}
		}
		private void OnNewSave(string filename)
		{
			switch (Config.Wipe.Mode)
			{
				case RootConfig.WipeConfig.Modes.Everything:
					Data = new RootData();
					break;

				case RootConfig.WipeConfig.Modes.MarketplaceListings:
					Data.GetMarketplaceFeed().Posts.Clear();
					break;

				case RootConfig.WipeConfig.Modes.PostsWithAudio:
					foreach (var feed in Data.Feeds)
					{
						foreach (var post in feed.Posts.ToArray())
						{
							if (post.CassetteId != 0)
								feed.Posts.Remove(post);
						}
					}
					break;
			}
		}
		private object OnTeamUpdate(ulong currentTeam, ulong newTeam, BasePlayer player)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			if (!Config.DMs.AutoTeamGroups) return null;

			Data.UpdateTeamConversation(RelationshipManager.ServerInstance.FindTeam(currentTeam));
			return null;
		}
		private object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			if (!Config.DMs.AutoTeamGroups) return null;

			var conversation = Data.GetTeamConversation(team);
			conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, player.userID, "Left the team.", true));

			if (team.teamLeader == player.userID) Data.DeleteTeamConversation(team);
			return null;
		}
		private object OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
		{
			if (!player.userID.IsSteamId() || player.IsNpc) return null;

			if (!Config.DMs.AutoTeamGroups) return null;

			var conversation = Data.GetTeamConversation(team);
			var userTarget = Data.GetUser(target);

			conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, player.userID, $"{userTarget.GetDisplayName()} got kicked from the team.", true));

			return null;
		}
		private object OnItemRepair(BasePlayer player, Item item)
		{
			if (item.skin == RusterShortFlipbookSkinId ||
				item.skin == RusterMediumFlipbookSkinId ||
				item.skin == RusterLongFlipbookSkinId)
			{
				try
				{
					var browser = GetBrowser(player);
					var flipbook = JsonConvert.DeserializeObject<RusterFlipbook>(item.text);
					var repairPrice = flipbook.Frames.Scale(0, flipbook.MaximumFrames, 0, Config.Ads.FlipbookResetPrice);
					if (browser.PlayerHasCurrency(repairPrice))
					{
						flipbook.Id = RandomEx.GetRandomString(20);
						flipbook.Frames = 0;
						flipbook.ThumbnailUrl = String.Empty;

						item.condition = item.maxCondition;
						item.text = JsonConvert.SerializeObject(flipbook);

						browser.TakePlayerCurrency(repairPrice);
						Print($"Your Flipbook has been wiped and you've been charged <color=orange>{Config.Currency.GetValueName(browser, repairPrice)}</color>.", player);
						Data.UpdateItem(item, browser.User);

						SendEffectTo(player, effect: "assets/bundled/prefabs/fx/repairbench/itemrepair.prefab");
						return false;
					}
					else
					{
						Print($"Couldn't wipe your Flipbook because you don't have the fee amount <color=orange>{Config.Currency.GetValueName(browser, repairPrice)}</color>.", player);
					}
				}
				catch { }
				return true;
			}

			return null;
		}
		private object OnShopCompleteTrade(ShopFront entity)
		{
			timer.In(1f, () =>
			{
				var vendor = entity.vendorPlayer;
				if (vendor == null) return;
				var customer = entity.customerPlayer;
				if (customer == null) return;

				var vendorBrowser = GetBrowser(vendor);
				var customerBrowser = GetBrowser(customer);
				var conversation = Data.GetConversation(vendorBrowser.ConversationId);
				if (conversation == null) return;
				var message = conversation.GetMessage(vendorBrowser.ConversationMessageId);
				if (message == null) return;
				message.IsTradeFinished = true;

				vendor.inventory.loot.Clear();
				vendor.inventory.loot.SendImmediate();
				customer.inventory.loot.Clear();
				customer.inventory.loot.SendImmediate();

				vendorBrowser.Draw();
				customerBrowser.Draw();

				vendorBrowser.Trade?.Kill();
				vendorBrowser.Trade = null;
				customerBrowser.Trade = null;
			});

			return null;
		}
		private object OnEntityVisibilityCheck(BaseEntity ent, BasePlayer player, uint id, string debugName, float maximumDistance)
		{
			var browser = GetBrowser(player);
			if (browser.Trade != null)
			{
				return true;
			}

			return null;
		}

		public void Log(object message, int level = 1)
		{
			if (level >= Config.LogLevel && message != null) Puts(message.ToString());
		}
		public void Print(object message, BasePlayer player = null, bool sendToRustPlusTeamChat = false)
		{
			if (player == null) PrintToChat($"<color=orange>{Name}</color>: {message}");
			else PrintToChat(player, $"<color=orange>{Name}</color> (OY): {message}");

			if (sendToRustPlusTeamChat)
				try { player.Team?.BroadcastTeamChat(player.userID, "Ruster.NET", message?.ToString(), ""); } catch { }
		}

		#endregion

		#region I/O

		public string GetTempFolder()
		{
			var folder = $"{Interface.Oxide.InstanceDirectory}{Path.DirectorySeparatorChar}temp";
			OsEx.Folder.Create(folder);

			return folder;
		}

		#endregion

		#region SQL

		private Core.MySql.Libraries.MySql MySQL = Interface.Oxide.GetLibrary<Core.MySql.Libraries.MySql>();
		private Core.Database.Connection SQLConnection;

		public void InitializeSQL()
		{
			Log("Initializing SQL database...");

			SQLConnection = MySQL.OpenDb(
				host: Config.Sql.Hostname,
				port: Config.Sql.Port,
				database: Config.Sql.Database,
				user: Config.Sql.Username,
				password: Config.Sql.Password + ";Connection Timeout=1;CharSet=utf8mb4",
				plugin: this);
		}
		public void UninitializeSQL()
		{
			SQLConnection?.Con?.Dispose();
		}

		public void LoadSQL()
		{
			Log("Loading SQL database...");

			try { SQLConnection?.Con?.Open(); } catch (Exception exception) { Log($"The SQL database couldn't have been loaded: {exception.Message}"); return; }

			MySQL.Insert(Core.Database.Sql.Builder.Append("SET NAMES utf8mb4"), SQLConnection);
			MySQL.Insert(Core.Database.Sql.Builder.Append($"CREATE TABLE IF NOT EXISTS {Config.Sql.Table} ( `json` LONGTEXT NOT NULL );"), SQLConnection);

			var sqlString = Core.Database.Sql.Builder.Append($"SELECT json FROM {Config.Sql.Table}");
			MySQL.Query(sqlString, SQLConnection, list =>
			{
				if (list != null)
				{
					Data = JsonConvert.DeserializeObject<RootData>(list[0]["json"].ToString().Replace("@@", "@").Replace("\\'", "'"));
				}
			});

		}
		public void SaveSQL()
		{
			if (Config.DataType != RootConfig.DataTypes.SQL) return;

			if (SQLConnection == null || SQLConnection.Con == null)
			{
				Log("Couldn't save SQL database since the connection is invalid.");
				return;
			}

			Log("Saving SQL database...");

			MySQL.Insert(Core.Database.Sql.Builder.Append($"CREATE TABLE IF NOT EXISTS {Config.Sql.Table} ( `json` LONGTEXT NOT NULL );"), SQLConnection);
			MySQL.Insert(Core.Database.Sql.Builder.Append($"TRUNCATE {Config.Sql.Table};"), SQLConnection);
			MySQL.Insert(Core.Database.Sql.Builder.Append($"INSERT INTO {Config.Sql.Table} (`json`) VALUES ('{JsonConvert.SerializeObject(Data).Replace("@", "@@").Replace("'", "\\'")}');"), SQLConnection);
		}

		#endregion

		#region CUI

		public Dictionary<ulong, RusterBrowser> Browsers { get; set; } = new Dictionary<ulong, RusterBrowser>();

		public List<RusterEmoji> Emojis { get; } = new List<RusterEmoji>(GetDefaultEmojis());

		public static RusterEmoji[] GetDefaultEmojis()
		{
			return new RusterEmoji[]
			{
				new RusterEmoji("None", "", @"https://cdn.discordapp.com/attachments/844914604080889867/849800843548950590/ruster_emoji_none.png"),
				new RusterEmoji("Star", "star", @"https://cdn.discordapp.com/attachments/844914604080889867/849815882405904415/ruster_emoji_star.png"),
				new RusterEmoji("Poop", "shit", @"https://cdn.discordapp.com/attachments/844914604080889867/849824498698289183/ruster_emoji_shit.png"),

				new RusterEmoji("Thumbs Up", "thumbsup", @"https://cdn.discordapp.com/attachments/844914604080889867/849780440420188180/ruster_emoji_thumbsup.png"),
				new RusterEmoji("Thumbs Down", "thumbsdown", @"https://cdn.discordapp.com/attachments/844914604080889867/849780437643165747/ruster_emoji_thumbsdown.png"),
				new RusterEmoji("Checkmark", "checkmark", @"https://cdn.discordapp.com/attachments/844914604080889867/849939586398421022/ruster_emoji_checkmark.png"),
				new RusterEmoji("X", "x", @"https://cdn.discordapp.com/attachments/844914604080889867/849939588013228032/ruster_emoji_x.png"),
				new RusterEmoji("Magnifying Glass", "mglass", @"https://cdn.discordapp.com/attachments/844914604080889867/857405940647198740/ruster_emoji_magnifyingglass.png"),
				new RusterEmoji("Attachment", "attach", @"https://cdn.discordapp.com/attachments/844914604080889867/853039554583592990/ruster_emoji_attachment.png"),
				new RusterEmoji("Pin", "pin", @"https://cdn.discordapp.com/attachments/844914604080889867/856496558500872202/ruster_emoji_pin.png"),
				new RusterEmoji("Voice", "voice", @"https://cdn.discordapp.com/attachments/844914604080889867/856598906851164180/ruster_emoji_voice.png"),

				new RusterEmoji("OK", "ok", @"https://cdn.discordapp.com/attachments/844914604080889867/849780438800531486/ruster_emoji_ok.png"),
				new RusterEmoji("Wave", "hello", @"https://cdn.discordapp.com/attachments/844914604080889867/849781678997372958/ruster_emoji_hello.png"),
				new RusterEmoji("Fuck You", "middlefinger", @"https://cdn.discordapp.com/attachments/844914604080889867/849780441913098240/ruster_emoji_middlefinger.png"),

				new RusterEmoji("Smile", "smile", @"https://cdn.discordapp.com/attachments/844914604080889867/849814292945174548/ruster_emoji_smile.png"),
				new RusterEmoji("ROFL", "rofl", @"https://cdn.discordapp.com/attachments/844914604080889867/852357293194477578/ruster_emoji_rofl.png"),
				new RusterEmoji("Hearts", "hearts", @"https://cdn.discordapp.com/attachments/844914604080889867/849814995909738526/ruster_emoji_hearts.png"),
				new RusterEmoji("Evil", "evil", @"https://cdn.discordapp.com/attachments/844914604080889867/849815595024384040/ruster_emoji_evil.png"),
				new RusterEmoji("Crazy", "crazy", @"https://cdn.discordapp.com/attachments/844914604080889867/849823409151803473/ruster_emoji_crazy.png"),
				new RusterEmoji("Clown", "clown", @"https://cdn.discordapp.com/attachments/844914604080889867/849827142162448414/ruster_emoji_clown.png"),

				new RusterEmoji("Heart", "heart", @"https://cdn.discordapp.com/attachments/844914604080889867/849826288516595712/ruster_emoji_heart.png"),
				new RusterEmoji("Sparkly Heart", "sparklyheart", @"https://cdn.discordapp.com/attachments/844914604080889867/849826290316214312/ruster_emoji_sparklyheart.png"),
				new RusterEmoji("Rainbow", "rainbow", @"https://cdn.discordapp.com/attachments/844914604080889867/849828451439476766/ruster_emoji_rainbow.png"),
				new RusterEmoji("Flipbook", "flipbook", @"https://cdn.discordapp.com/attachments/844914604080889867/938980271821324339/ruster_evil_flipbook.png"),

				new RusterEmoji("Peach", "peach", @"https://cdn.discordapp.com/attachments/844914604080889867/849816219502116934/ruster_emoji_peach.png"),
				new RusterEmoji("Eggplant", "eggplant", @"https://cdn.discordapp.com/attachments/844914604080889867/849819703882154045/ruster_emoji_eggplant.png"),
				new RusterEmoji("Lollipop", "lollipop", @"https://cdn.discordapp.com/attachments/844914604080889867/849820060926083092/ruster_emoji_lollipop.png"),
				new RusterEmoji("Rocket", "rocket", @"https://cdn.discordapp.com/attachments/844914604080889867/849820634493091860/ruster_emoji_rocket.png"),
				new RusterEmoji("Flower", "flower", @"https://cdn.discordapp.com/attachments/844914604080889867/849820999331086386/ruster_emoji_flower.png"),

				new RusterEmoji("Rust", "rust", @"https://cdn.discordapp.com/attachments/844914604080889867/849825599672418324/ruster_emoji_rust.png"),
				new RusterEmoji("Literally", "lrb", @"https://cdn.discordapp.com/attachments/844914604080889867/849823846488211476/ruster_emoji_lrb.png"),
				new RusterEmoji("Codefling", "codefling", @"https://cdn.discordapp.com/attachments/844914604080889867/849822716471410719/ruster_emoji_cf.png"),
				new RusterEmoji("Codefling+", "codeflingplus", @"https://cdn.discordapp.com/attachments/844914604080889867/935552093975089222/ruster_emoji_cfplus.png"),
				new RusterEmoji("Jamie Lee", "jamielee", @"https://cdn.discordapp.com/attachments/844914604080889867/935553176894079026/ruster_emoji_jamielee.png"),
				new RusterEmoji("Jackie Chan", "jackiechan", @"https://cdn.discordapp.com/attachments/844914604080889867/936797548994252820/ruster_emoji_jackiechan.png"),

				new RusterEmoji("KEKW", "kekw", @"https://cdn.discordapp.com/attachments/844914604080889867/935555402786021437/ruster_emoji_kekw.png"),
				new RusterEmoji("Meuf", "meuf", @"https://cdn.discordapp.com/attachments/844914604080889867/935564151252742244/ruster_emoji_meuf.png"),

			};
		}
		public static string[] GetDefaultAvatars()
		{
			return new string[]
			{
				"https://cdn.discordapp.com/attachments/844914604080889867/889902410003251220/ruster_vendor_vacay.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/889902410955382825/ruster_vendor_rod.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/889902411680981012/ruster_vendor_raul.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/889902413857833030/ruster_vendor_bandit.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/940030093928063016/ruster_vendor_vacay_female.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/940030093676400681/ruster_vendor_rod_female.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/940030093487640606/ruster_vendor_raul_female.jpg",
				"https://cdn.discordapp.com/attachments/844914604080889867/940030094196502588/ruster_vendor_bandit_female.jpg"
			};
		}
		public static string[] GetDefaultBanners()
		{
			return new string[]
			{
				"https://cdn.discordapp.com/attachments/844914604080889867/940219142231949312/RustClient_yhjZdgAtpR_1.png",
				"https://cdn.discordapp.com/attachments/844914604080889867/940219537377349763/RustClient_yhjZdgAtpR_2.png",
				"https://cdn.discordapp.com/attachments/844914604080889867/940219786850353152/RustClient_yhjZdgAtpR_3.png"
			};
		}
		public string GetEmojiUrl(string name)
		{
			return GetEmoji(name)?.IconUrl;
		}
		public RusterEmoji GetEmoji(string name)
		{
			return Emojis.FirstOrDefault(x => x.Shortname == name);
		}

		public RusterUser[] EmptyUserArray = new RusterUser[0];

		public static string GetStringChecksum(string value)
		{
			if (value.Length <= 3) return null;

			using (var md5 = MD5.Create())
			{
				var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
				return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			}
		}

		public RusterBrowser GetBrowser(BasePlayer player)
		{
			if (!Browsers.ContainsKey(player.userID))
			{
				var browser = new RusterBrowser(player);
				browser.SetLanguage(browser.User.Configuration.Language);

				Browsers.Add(player.userID, browser);
				return browser;
			}
			else
			{
				var browser = Browsers[player.userID];
				browser.Player = player;
				browser.User = Data.GetUser(player);
				browser.SetLanguage(browser.User.Configuration.Language);

				return Browsers[player.userID];
			}
		}
		public RusterBrowser GetBrowser(ulong playerId)
		{
			var browser = (RusterBrowser)null;
			if (!Browsers.ContainsKey(playerId))
			{
				browser = new RusterBrowser(playerId);
				browser.SetLanguage(browser.User.Configuration.Language);

				Browsers.Add(playerId, browser);
				return browser;
			}

			browser = Browsers[playerId];
			browser.SetLanguage(browser.User.Configuration.Language);
			return browser;
		}
		public RusterBrowser GetBrowser(RusterUser user)
		{
			if (user == null) return null;

			var browser = (RusterBrowser)null;
			if (!Browsers.ContainsKey(user.Id))
			{
				browser = new RusterBrowser(user.Id);
				browser.User = user;
				browser.SetLanguage(browser.User.Configuration.Language);

				Browsers.Add(user.Id, browser);
				return browser;
			}

			browser = Browsers[user.Id];
			browser.User = user;
			browser.SetLanguage(browser.User.Configuration.Language);
			return browser;
		}

		public class RusterBrowser
		{
			public bool IsOnline { get; set; } = true;

			#region Keys

			public const string BackgroundCUI = "rusterbg";

			public const string MainCUI = "ruster";
			public const string MainGhostCUI = MainCUI + ".ghost";
			public const string SplashCUI = MainCUI + ".splash";
			public const string SplashGhostCUI = SplashCUI + ".ghost";
			public const string NoticeCUI = MainCUI + ".notice";
			public const string NoticeGhostCUI = NoticeCUI + ".ghost";
			public const string ProfileCUI = MainCUI + ".profile";
			public const string ProfileGhostCUI = ProfileCUI + ".ghost";
			public const string FullPostCUI = MainCUI + ".post";
			public const string FullPostGhostCUI = FullPostCUI + ".ghost";
			public const string MessageReactionCUI = MainCUI + ".messagereaction";
			public const string MessageReactionGhostCUI = MessageReactionCUI + ".ghost";
			public const string ScreenNoticeCUI = MainCUI + ".restockingnotice";
			public const string ScreenNoticeGhostCUI = ScreenNoticeCUI + ".ghost";
			public const string ChatBalloonMessagesNoticeCUI = MainCUI + ".chatballoonmessages";
			public const string ChatBalloonMessagesNoticeGhostCUI = ChatBalloonMessagesNoticeCUI + ".ghost";
			public const string ConfirmDialogCUI = MainCUI + ".confirmdialog";
			public const string ConfirmDialogGhostCUI = ConfirmDialogCUI + ".ghost";
			public const string LanguageDialogCUI = MainCUI + ".languagedialog";
			public const string LanguageDialogGhostCUI = LanguageDialogCUI + ".ghost";
			public const string PostLikesAndDislikesDialogCUI = MainCUI + ".postlikesdislikes";
			public const string PostLikesAndDislikesDialogGhostCUI = PostLikesAndDislikesDialogCUI + ".ghost";
			public const string UsersDialogCUI = MainCUI + ".usersdialog";
			public const string UsersDialogGhostCUI = UsersDialogCUI + ".ghost";
			public const string ContactsCUI = MainCUI + ".contacts";
			public const string ContactsGhostCUI = ContactsCUI + ".ghost";
			public const string UploadAudioDialogCUI = MainCUI + ".uploadaudio";
			public const string UploadAudioDialogGhostCUI = UploadAudioDialogCUI + ".ghost";
			public const string AudioPlayerCUI = MainCUI + ".audioplayer";
			public const string AudioPlayerGhostCUI = AudioPlayerCUI + ".ghost";
			public const string HashtagFilterCUI = MainCUI + ".hashtagfilter";
			public const string HashtagFilterGhostCUI = HashtagFilterCUI + ".ghost";
			public const string ServerViewerListDialogCUI = MainCUI + ".postlikesdislikes";
			public const string ServerViewerListDialogGhostCUI = ServerViewerListDialogCUI + ".ghost";
			public const string StoryDialogCUI = MainCUI + ".story";
			public const string PictureViewerDialogCUI = MainCUI + ".pictureviewer";
			public const string PictureViewerDialogGhostCUI = PictureViewerDialogCUI + ".ghost";
			public const string GifPanelDialogCUI = MainCUI + ".gifpanel";
			public const string GifPanelDialogGhostCUI = GifPanelDialogCUI + ".ghost";
			public const string GifPanelContentCUI = GifPanelDialogCUI + ".content";
			public const string TextEditorDialogCUI = MainCUI + ".texteditor";
			public const string TextEditorDialogGhostCUI = TextEditorDialogCUI + ".ghost";
			public const string UserSettingsCUI = MainCUI + ".usersettings";
			public const string UserSettingsGhostCUI = UserSettingsCUI + ".ghost";
			public const string ColorPickerCUI = MainCUI + ".colorpicker";
			public const string ColorPickerGhostCUI = ColorPickerCUI + ".ghost";
			public const string CouponEditorCUI = MainCUI + ".couponeditor";
			public const string CouponEditorGhostCUI = CouponEditorCUI + ".ghost";
			public const string CouponListCUI = MainCUI + ".couponlist";
			public const string CouponListGhostCUI = CouponListCUI + ".ghost";
			public const string TransactionListCUI = MainCUI + ".transactionlist";
			public const string TransactionListGhostCUI = TransactionListCUI + ".ghost";
			public const string UserPhotoListCUI = MainCUI + ".userphotolist";
			public const string UserPhotoListGhostCUI = UserPhotoListCUI + ".ghost";
			public const string ModalCUI = MainCUI + ".modal";
			public const string ModalGhostCUI = ModalCUI + ".ghost";

			#endregion

			#region Colors

			public const string DefaultFont = "robotocondensed-regular.ttf";
			public const string CloseButtonColor = "1 0.3 0 1";
			public const string NormalButtonColor = "0.7 0.85 0.1 1";
			public const string DeselectedButtonColor = "0.3 0.3 0.3 0.4";
			public const string ArrowButtonColor = "0.2 0.2 0.2 0.9";
			public const string EwllowButtonColor = "0.9 0.8 0.4 0.9";
			public const string HashtagColor = "#cc9d1b";
			public const string MentionColor = "#97cc1b";

			#endregion

			#region Images

			public static string RusterNetworkLogo => Instance.GetProConfig("RusterNetworkLogo",
				Instance.Config.Features.GetHolidayMode() == RootConfig.FeaturesConfig.HolidayModes.Halloween ? "https://cdn.discordapp.com/attachments/844914604080889867/893629408932409404/ruster_full_loading_halloween.png" :
				Instance.Config.Features.GetHolidayMode() == RootConfig.FeaturesConfig.HolidayModes.Christmas ? "https://cdn.discordapp.com/attachments/844914604080889867/921577008583036968/ruster_low_loading_christmas.png" :
				"https://cdn.discordapp.com/attachments/844914604080889867/941150137403707402/rusterv3_fhl.png");
			public static string RusterLogo => Instance.GetProConfig("RusterLogo",
				Instance.Config.Features.GetHolidayMode() == RootConfig.FeaturesConfig.HolidayModes.Halloween ? "https://cdn.discordapp.com/attachments/844914604080889867/893624179176382514/ruster_full_halloween.png" :
				Instance.Config.Features.GetHolidayMode() == RootConfig.FeaturesConfig.HolidayModes.Christmas ? "https://cdn.discordapp.com/attachments/844914604080889867/921577460359909386/ruster_full_christmas.png" :
				"https://cdn.discordapp.com/attachments/844914604080889867/941150138456506388/rusterv3_ll.png");
			public static string RusterMarketplaceLogo => Instance.GetProConfig("RusterMarketplaceLogo", "https://cdn.discordapp.com/attachments/844914604080889867/941150137651179580/ruster_full2_v3_l.png");
			public static string RusterMarketplace2Logo => Instance.GetProConfig("RusterMarketplace2Logo",
				Instance.Config.Features.GetHolidayMode() == RootConfig.FeaturesConfig.HolidayModes.Halloween ? "https://cdn.discordapp.com/attachments/844914604080889867/893624731629158420/ruster_marketplace2_halloween_full.png" :
				Instance.Config.Features.GetHolidayMode() == RootConfig.FeaturesConfig.HolidayModes.Christmas ? "https://cdn.discordapp.com/attachments/844914604080889867/921578253884481607/ruster_marketplace2_low_christmas.png" :
				"https://cdn.discordapp.com/attachments/844914604080889867/941154322203410452/ruster_full_v3_l_1.png");
			public static string RusterFMLogo => Instance.GetProConfig("RusterFMLogo", "https://cdn.discordapp.com/attachments/844914604080889867/856880620528730112/ruster_fm.png");
			public static string VerifiedTickUrl => Instance.GetProConfig("RusterVerifiedTickIcon", "https://cdn.discordapp.com/attachments/844914604080889867/845000547366469634/verified_l.png");
			public static string RusterStoriesLogo => Instance.GetProConfig("RusterStoriesLogo", "https://cdn.discordapp.com/attachments/844914604080889867/874797147411865640/ruster_stories.png");
			public static string RusterStoriesFullLogo => Instance.GetProConfig("RusterStoriesFullLogo", "https://cdn.discordapp.com/attachments/844914604080889867/874797146128404500/ruster_stories_full.png");
			public const string CodeflingRustAPI = "https://codefling.com/gamedb/rust/items";
			public const string EclipseUrl = "https://cdn.discordapp.com/attachments/844914604080889867/874816395735601152/eclipse.png";
			public const string NoImageUrl = "https://cdn.discordapp.com/attachments/844914604080889867/878463806815227984/no_image.jpg";
			public const string BannerNoiseUrl = "https://cdn.discordapp.com/attachments/844914604080889867/940342487984275567/noise_2.png";
			public const string ClearBanner = "https://cdn.discordapp.com/attachments/844914604080889867/940095981117132841/RustClient_yhjZdgAtpR.png";

			public const string SenderInitialBalloonUrl = "https://cdn.discordapp.com/attachments/844914604080889867/849359077595545620/sender_initial.png";
			public const string SenderFurtherBalloonUrl = "https://cdn.discordapp.com/attachments/844914604080889867/849359075954524160/sender_further.png";
			public const string SentInitialBalloonUrl = "https://cdn.discordapp.com/attachments/844914604080889867/849359074553233429/sent_initial.png";
			public const string SentFurtherBalloonUrl = "https://cdn.discordapp.com/attachments/844914604080889867/849359073329545286/sent_further.png";

			public const string SentCheckUrl = "https://cdn.discordapp.com/attachments/844914604080889867/849404305240359002/sent_check.png";
			public const string ReadCheckUrl = "https://cdn.discordapp.com/attachments/844914604080889867/849404306690932756/read_check.png";
			public const string NotifyUrl = "https://cdn.discordapp.com/attachments/844914604080889867/850159202679914516/ruster_notify.png";
			public const string MagnifyingGlass = "https://cdn.discordapp.com/attachments/844914604080889867/857405940647198740/ruster_emoji_magnifyingglass.png";
			public const string GearUrl = "https://cdn.discordapp.com/attachments/844914604080889867/934477718093967400/gear.png";
			public const string ShoppingCartUrl = "https://cdn.discordapp.com/attachments/844914604080889867/940688380759515186/shooping.png";
			public const string BinUrl = "https://cdn.discordapp.com/attachments/844914604080889867/938952851567300638/bin.png";
			public const string FlipbookIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/938980271821324339/ruster_evil_flipbook.png";

			public const string AddReactionIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852137563863121920/ruster_add_reaction.png";
			public const string ScreenshotIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/853039554583592990/ruster_emoji_attachment.png";
			public const string PlayButtonIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/855424319769280552/ruster_emoji_play.png";
			public const string PauseButtonIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/855427314569838602/ruster_emoji_pause.png";
			public const string StopButtonIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/855428327166967838/ruster_emoji_stop.png";
			public const string PinButtonIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/856496558500872202/ruster_emoji_pin.png";
			public const string VoiceButtonIconUrl = "https://cdn.discordapp.com/attachments/844914604080889867/856598906851164180/ruster_emoji_voice.png";

			public const string MarketplaceBackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852977087803424788/ruster_bg_marketplace.png";
			public const string UserItemsBackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852977087803424788/ruster_bg_marketplace.png";
			public const string MyMarketplaceBackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852977091166732328/ruster_bg_mymarketplace.png";
			public const string CommunityFeedBackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852985854128553984/ruster_bg_communityfeed.png";
			public const string UserFeedBackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852985855266390066/ruster_bg_userfeed.png";
			public const string PostBackgroundUrl = "https://cdn.discordapp.com/attachments/844914604080889867/852991437787365436/ruster_bg_post.png";

			public const string TestAvatarFrameUrl = "https://cdn.discordapp.com/attachments/844914604080889867/940379058888376361/ruster_vendor_rod_female_4.png";

			#endregion

			#region Properties

			public const int PostsPerPage = 5;
			public const int FriendRequestsPerPage = 3;
			public const int FriendsPerPage = 6;
			public const int ConversationsPerPage = 13;
			public const int DMsPerPage = 9;
			public const int EmojisPerPage = 9;
			public const int LanguagesPerPage = 9;
			public const int PostLength = 125;
			public const int ServersPerPage = 3;
			public const int StoriesPerPage = 7;
			public const int NotificationsPerPage = 5;

			public const float MainFadeout = 0.075f * 1.75f;

			public PanelTypes PanelType { get; set; } = PanelTypes.None;
			public OverlayPanelTypes OverlayPanelType { get; set; } = OverlayPanelTypes.None;
			public class Page
			{
				public int Id { get; set; } = 0;
				public int CurrentPage { get; set; }
				public int TotalPages { get; set; }
				public RusterHashtag CurrentHashtag { get; set; }
				public bool IsCustomFilter { get; set; }

				public void Check()
				{
					if (TotalPages >= 0 && CurrentPage < 0) { CurrentPage = 0; }
					if (CurrentPage > TotalPages) CurrentPage = TotalPages;
				}

				public Page() { }
				public Page(int id) { Id = id; }
			}

			public RusterModal Modal { get; set; }

			public bool PendingSlotConfirmation { get; set; } = false;
			public ulong MainFeedId { get; set; } = 0;
			public bool IsOpen { get; set; }
			public ulong FeedId { get; set; } = 0;
			public int PostId { get; set; } = 0;
			public string Content { get; set; }
			public bool UploadingStory { get; set; }
			public bool UploadingPhotograph { get; set; }
			public string UploadedPhotograph { get; set; }
			public string UploadedPhotographTag { get; set; }
			public RusterFeed.RusterPost.RusterGif PostGif { get; set; }
			public ItemContainer PhotographContainer { get; set; }
			public ItemContainer SellingContainer { get; set; }
			public ItemContainer CassetteContainer { get; set; }
			public ItemContainer GiftBasketContainer { get; set; }
			public ShopFront Trade { get; set; }
			public ulong UploadedCassetteId { get; set; }
			public string UploadedCassetteTitle { get; set; }
			public bool Location { get; set; }
			public RusterMarketplaceListing MarketplaceItem { get; set; }
			public RusterFeed.RusterPost.RusterPoll Poll { get; set; }
			public bool IsAdvertPost { get; set; } = false;
			public bool Is24hAdvert { get; set; } = false;
			public bool WholeStack { get; set; } = false;
			public bool FullRestockMode { get; set; }
			public int? RestockedPostId { get; set; } = null;
			public int ConversationId { get; set; } = 0;
			public int ConversationMessageId { get; set; } = 0;
			public string ConversationMessage { get; set; } = "";
			public int CurrentStackAmount { get; set; } = 1;
			public bool IsBusy { get; set; } = false;
			public string UploadAudioUrl { get; set; }
			public string UploadAudioSkip { get; set; }
			public string HashtagFilterInput { get; set; }
			public int HashtagBrowserPage { get; set; }
			public string ContactsFilter { get; set; } = string.Empty;
			public string ContactsCommand { get; set; }
			public Timer LaunchConfirmationTimer { get; set; }

			public ulong CustomFeed1 { get; set; } = 0;
			public ulong CustomFeed2 { get; set; } = 0;

			public Dictionary<int, Page> Pages { get; set; } = new Dictionary<int, Page>();
			public Action CurrentAction { get; set; }
			public ulong CurrentUserId { get; set; }

			public Action OnDialongCancel { get; set; }
			public Action OnDialogAccept { get; set; }
			public Action<RusterBrowser> OnNotificationRead { get; set; }

			public Action<string> OnTextEditorSubmit { get; set; }
			public Action OnTextEditorCancel { get; set; }
			public string TextEditorContent { get; set; }
			public string TextEditorOriginalContent { get; set; }
			public Func<string, string, KeyValuePair<bool, string>> TextEditorCanSubmit { get; set; }

			public bool IsCooledDown { get; set; }
			public bool IsGlobalBackground { get; set; }
			public RusterStory ViewingStory { get; set; }

			public bool IsViewingPurchases { get; set; } = true;

			public RusterAudioPlayer AudioPlayer { get; set; }
			public RusterServerViewer ServerViewer { get; set; } = new RusterServerViewer();
			public RusterGIFProcessor GifProcessor { get; set; }

			public TimeSince ButtonPressCooldown { get; set; } = new TimeSince();
			public TimeSince BusinessCardCooldown { get; set; } = new TimeSince();
			public TimeSince RustPlusNotificationCooldown { get; set; } = new TimeSince();

			public float ColorBrightness { get; set; } = 1f;
			public Action<string, string> OnColorPicked { get; set; }
			public Action OnColorCancel { get; set; }

			public RusterCoupon EditingCoupon { get; set; } = new RusterCoupon
			{
				Discount = 20f,
				Code = $"COUP{Date.Current.Year:0000}"
			};
			public Action<RusterCoupon> OnCouponEditorSaved { get; set; }
			public Action OnCouponEditorClear { get; set; }

			public Action<string> OnUserPhotoSelected { get; set; }
			public Action OnUserPhotoClear { get; set; }

			public Action<RusterUser> OnUserSelected { get; set; }

			public Item TemporaryHeldItem { get; set; }
			public ItemContainer HeldItemContainer { get; set; }

			public Dictionary<string, Timer> CoverTimers { get; set; } = new Dictionary<string, Timer>();

			public void RestoreHeldItem()
			{
				if (HeldItemContainer != null)
				{
					TemporaryHeldItem.Remove();
					foreach (var item in HeldItemContainer.itemList.ToArray()) item.MoveToContainer(Player.inventory.containerBelt);

					HeldItemContainer.Kill();
					HeldItemContainer = null;

					Player.inventory.containerBelt.SetLocked(false);
				}
			}

			public enum PanelTypes
			{
				None,
				NewPost,
				PollEditor,
				Post,
				DirectMessages,
				CustomFeeds
			}
			public enum OverlayPanelTypes
			{
				None,
				Profile,
				MessageReaction
			}

			#endregion

			#region References

			public BasePlayer Player { get; set; }
			public RusterUser User { get; set; }
			private CuiElementContainer Container { get; set; }
			private string Background { get; set; }

			#endregion

			public RusterBrowser(BasePlayer player)
			{
				if (player == null) return;

				Player = player;
				User = Instance.Data.GetUser(player);
				AudioPlayer = new RusterAudioPlayer(this);
				GifProcessor = new RusterGIFProcessor { Browser = this };
			}
			public RusterBrowser(ulong playerId)
			{
				User = Instance.Data.GetUser(playerId);
				AudioPlayer = new RusterAudioPlayer(this);
				GifProcessor = new RusterGIFProcessor { Browser = this };
			}

			public void Refund()
			{
				if (Player == null) return;

				if (PhotographContainer != null) { foreach (var item in PhotographContainer.itemList) Player.GiveItem(item); PhotographContainer.Kill(); PhotographContainer = null; }
				if (SellingContainer != null) { foreach (var item in SellingContainer.itemList) Player.GiveItem(item); SellingContainer.Kill(); SellingContainer = null; }

				Clear();
			}
			public void Clear()
			{
				MarketplaceItem = null;
				PhotographContainer?.Kill(); PhotographContainer = null;
				SellingContainer?.Kill(); SellingContainer = null;
			}

			public void CloseFully()
			{
				PanelType = PanelTypes.None;
				OverlayPanelType = OverlayPanelTypes.None;
				User.Configuration.GiftTarget = null;

				if (Player != null)
				{
					UnlockPlayer();
					Trade?.Kill();
					Trade = null;

					Close();
					CloseCover();
					CloseCover("fullpost");
					CloseCover("profile");

					foreach (var timer in CoverTimers)
					{
						timer.Value?.Destroy();
						CuiHelper.DestroyUi(Player, $"{BackgroundCUI}_{timer.Key}");
					}

					AudioPlayer.Stop(false);

					ConversationId = 0;
					MainFeedId = 0;

					Instance.RusterAddons?.Call("RNETAPI_OnBrowserClose", Player.userID);
				}

				IsOpen = false;
			}

			public const int CloseRepeater = 2;
			public Dictionary<RusterFeed.RusterPost, RusterFeed> FullPostHistory { get; set; } = new Dictionary<RusterFeed.RusterPost, RusterFeed>();
			public const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			#region Closing

			public void CloseCover(string id = "main")
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, $"{BackgroundCUI}_{id}");
				}
			}
			public void Close()
			{
				IsOpen = false;

				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, MainCUI);
					CuiHelper.DestroyUi(Player, MainGhostCUI);
				}

				if (PhotographContainer != null)
				{
					PhotographContainer?.Kill();
					PhotographContainer = null;
				}

				CloseProfile();
				CloseFullPosts();
				CloseMessageReaction();
				CloseConfirmDialog();
				CloseScreenNotice();
				CloseChatBalloonMessages();
				CloseLanguageDialog();
				ClosePostLikesAndDislikes();
				CloseContacts();
				CloseUploadAudio();
				CloseHashtagFilter();
				CloseServerViewerList();
				CloseStory();
				ClosePictureViewer();
				CloseBlank();
				CloseTextEditor();
				CloseUserSettings();
				CloseColorPicker();
				CloseCouponEditor();
				CloseCouponList();
				CloseTransactionList();
				CloseUserPhotoList();
				CloseUsers();
				CloseModal();
			}
			public void CloseNotice()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, NoticeCUI);
					CuiHelper.DestroyUi(Player, NoticeGhostCUI);
				}
			}
			public void CloseSplash()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, SplashCUI);
					CuiHelper.DestroyUi(Player, SplashGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseProfile()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ProfileCUI);
					CuiHelper.DestroyUi(Player, ProfileGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseFullPost(RusterFeed feed, RusterFeed.RusterPost post)
			{
				if (post == null) return;
				if (FullPostHistory.ContainsKey(post)) FullPostHistory.Remove(post);

				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, $"{FullPostCUI}_{post.Id}_{feed.Id}");
					CuiHelper.DestroyUi(Player, $"{FullPostGhostCUI}_{post.Id}_{feed.Id}");
				}

				UnlockPlayer();
			}
			public void CloseFullPosts()
			{
				foreach (var post in FullPostHistory.ToArray())
				{
					CloseFullPost(post.Value, post.Key);
				}

				UnlockPlayer();
			}
			public void CloseMessageReaction()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, MessageReactionCUI);
					CuiHelper.DestroyUi(Player, MessageReactionGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseConfirmDialog()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ConfirmDialogCUI);
					CuiHelper.DestroyUi(Player, ConfirmDialogGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseScreenNotice()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ScreenNoticeCUI);
					CuiHelper.DestroyUi(Player, ScreenNoticeGhostCUI);
				}
			}
			public void CloseChatBalloonMessages()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ChatBalloonMessagesNoticeCUI);
					CuiHelper.DestroyUi(Player, ChatBalloonMessagesNoticeGhostCUI);
				}
			}
			public void CloseLanguageDialog()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, LanguageDialogCUI);
					CuiHelper.DestroyUi(Player, LanguageDialogGhostCUI);
				}
			}
			public void ClosePostLikesAndDislikes()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, PostLikesAndDislikesDialogCUI);
					CuiHelper.DestroyUi(Player, PostLikesAndDislikesDialogGhostCUI);
				}
			}
			public void CloseUsers()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, UsersDialogCUI);
					CuiHelper.DestroyUi(Player, UsersDialogGhostCUI);
				}
			}
			public void CloseContacts()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ContactsCUI);
					CuiHelper.DestroyUi(Player, ContactsGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseUploadAudio()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, UploadAudioDialogCUI);
					CuiHelper.DestroyUi(Player, UploadAudioDialogGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseHashtagFilter()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, HashtagFilterCUI);
					CuiHelper.DestroyUi(Player, HashtagFilterGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseServerViewerList()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ServerViewerListDialogCUI);
					CuiHelper.DestroyUi(Player, ServerViewerListDialogGhostCUI);
				}
			}
			public void CloseStory()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, StoryDialogCUI);
				}
			}
			public void ClosePictureViewer()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, PictureViewerDialogCUI);
					CuiHelper.DestroyUi(Player, PictureViewerDialogGhostCUI);
				}
			}
			public void CloseBlank()
			{
				CloseBlankContent();

				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, GifPanelDialogCUI);
					CuiHelper.DestroyUi(Player, GifPanelDialogGhostCUI);
				}
			}
			public void CloseBlankContent()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, GifPanelContentCUI);
				}
			}
			public void CloseTextEditor(bool submit = false)
			{
				if (submit)
				{
					if (TextEditorCanSubmit != null && !TextEditorCanSubmit.Invoke(TextEditorOriginalContent, TextEditorContent).Key)
					{
						Notify("Cannot Submit!", TextEditorCanSubmit.Invoke(TextEditorOriginalContent, TextEditorContent).Value);
						return;
					}
					else
					{
						OnTextEditorSubmit?.Invoke(TextEditorContent);
					}
				}
				else
				{
					OnTextEditorCancel?.Invoke();
				}

				TextEditorCanSubmit = null;
				TextEditorContent = null;
				OnTextEditorSubmit = null;
				OnTextEditorCancel = null;

				UnlockPlayer();

				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, TextEditorDialogCUI);
					CuiHelper.DestroyUi(Player, TextEditorDialogGhostCUI);
				}
			}
			public void CloseUserSettings()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, UserSettingsCUI);
					CuiHelper.DestroyUi(Player, UserSettingsGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseColorPicker()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ColorPickerCUI);
					CuiHelper.DestroyUi(Player, ColorPickerGhostCUI);
				}
			}
			public void CloseCouponEditor()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, CouponEditorCUI);
					CuiHelper.DestroyUi(Player, CouponEditorGhostCUI);
				}

				UnlockPlayer();
			}
			public void CloseCouponList()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, CouponListCUI);
					CuiHelper.DestroyUi(Player, CouponListGhostCUI);
				}
			}
			public void CloseTransactionList()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, TransactionListCUI);
					CuiHelper.DestroyUi(Player, TransactionListGhostCUI);
				}
			}
			public void CloseUserPhotoList()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, UserPhotoListCUI);
					CuiHelper.DestroyUi(Player, UserPhotoListGhostCUI);
				}
			}
			public void CloseModal()
			{
				for (int i = 0; i < CloseRepeater; i++)
				{
					CuiHelper.DestroyUi(Player, ModalCUI);
					CuiHelper.DestroyUi(Player, ModalGhostCUI);
				}

				UnlockPlayer();
			}

			#endregion

			#region Main Drawing

			public void DrawCover(string id = "main", string title = null, bool autoClose = true, float? closeAfter = null)
			{
				var nethi = new CuiImageComponent();

				if (closeAfter == null) closeAfter = MainFadeout;

				if (string.IsNullOrEmpty(title)) title = GetPhrase("cov_loading");

				var ui = $"{BackgroundCUI}_{id}";
				CuiHelper.DestroyUi(Player, ui);
				if (!CoverTimers.ContainsKey(id)) CoverTimers.Add(id, null);
				else CoverTimers[id]?.Destroy();

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 0.7" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", ui);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);

				container.Add(new CuiLabel { Text = { Text = title, Color = "1 1 1 0.4", FontSize = 20, Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }, FadeOut = MainFadeout }, ratioBackground);

				CuiHelper.AddUi(Player, container);
				if (autoClose)
				{
					CoverTimers[id] = Instance.timer.In(closeAfter.Value, () => { CuiHelper.DestroyUi(Player, ui); });
				}
			}
			public void Draw(bool isBackground = false, Action onDraw = null)
			{
				if (PendingSlotConfirmation)
				{
					CloseScreenNotice();
					LaunchConfirmationTimer?.Destroy();
					LaunchConfirmationTimer = null;
				}
				PendingSlotConfirmation = false;

				if (!IsOnline)
				{
					Instance.Print("Ruster.NET is currently offline.", Player);
					CloseSplash();
					Close();
					return;
				}

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (isBackground)
				{
					if (IsCooledDown) return;
					IsCooledDown = true;
				}

				CloseSplash();
				Close();

				IsOpen = true;

				if (!isBackground)
				{
					Draw(isBackground: true, onDraw: onDraw);
					DrawCover(autoClose: false);
				}

				try
				{
					Container = new CuiElementContainer();
					var background = Container.Add(new CuiPanel { Image = { Color = $"0 0 0 0.1" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? MainCUI : MainGhostCUI);
					Container.Add(new CuiPanel { Image = { Color = $"0.1 0.1 0.1 {(User.Configuration.PerformanceMode ? 0.4 : (isBackground ? 0 : Instance.Config.Look.DisableBlur ? 0 : 0.4))}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, background);
					Container.Add(new CuiLabel { Text = { Text = $"Ruster.NET{(Instance.IsPro() ? " Pro" : "")} v{Instance.Version} (c) Raul-Sorin Sorban {Date.Current.Year} {(global::RusterNET.Core.Internet.GetChecksum().Truncate(6, ""))}", FontSize = 7, Color = "1 1 1 0.15", Font = DefaultFont, Align = TextAnchor.LowerLeft }, RectTransform = { AnchorMin = $"0.01 0.01", AnchorMax = $"1 1" } }, background);
					Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.5", }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, background);

					var ratioBackground = Container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
					Background = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.005 0.02", AnchorMax = "0.99 0.975" }, CursorEnabled = true }, ratioBackground);

					if (ServerViewer.IsViewing())
					{
						DrawUser();

						DrawFeed(ServerViewer.CommunityFeed, GetPage(0), canAddNewPost: true);
						DrawFeed(ServerViewer.MarketplaceFeed, GetPage(1), offset: 0.3f, canAddNewPost: true);

						DrawServerViewerBanner();
					}
					else
					{
						switch (PanelType)
						{
							case PanelTypes.PollEditor:
								DrawPollEditor(Poll);
								break;

							case PanelTypes.NewPost:
								DrawNewPost(Instance.Data.GetFeed(FeedId));
								break;

							case PanelTypes.DirectMessages:
								DrawUser();
								DrawDirectMessages();
								DrawNotificationTray();
								break;

							case PanelTypes.CustomFeeds:
								DrawUser();
								if (CustomFeed1 != 0) DrawFeed(Instance.Data.GetFeed(CustomFeed1), GetPage(0), canAddNewPost: true);
								if (CustomFeed2 != 0) DrawFeed(Instance.Data.GetFeed(CustomFeed2), GetPage(1), offset: 0.3f, canAddNewPost: true);
								DrawNotificationTray();
								break;

							default:
								DrawUser();

								var feed = (RusterFeed)null;
								var marketplaceFeed = Instance.Data.GetMarketplaceFeed().GetTemporaryCopy();

								switch (MainFeedId)
								{
									case MarketplaceFeedId:
										var userFeed = Instance.Data.GetFeed(User);
										feed = Instance.Data.GetFeed(User).GetTemporaryCopy();
										feed.AllowAdverts = false;
										feed.Title = GetPhrase("selfshop");
										feed.BackgroundUrl = MyMarketplaceBackgroundUrl;
										feed.Posts = marketplaceFeed.Posts.Where(x => !x.IsAdvert() && x.UserId == User.Id && x.IsMarketplaceListing()).ToList();
										feed.Posts.InsertRange(0, User.GetAdvertPosts());
										feed.AllowPinning = false;
										feed.HexBackgroundColor = userFeed.HexBackgroundColor;
										feed.HexDarkSubtitleColor = userFeed.HexDarkSubtitleColor;
										feed.HexDarkTitleColor = userFeed.HexDarkTitleColor;
										feed.HexSubtitleColor = userFeed.HexSubtitleColor;
										feed.HexTitleColor = userFeed.HexTitleColor;

										marketplaceFeed.Posts = marketplaceFeed.Posts.Where(x => !x.IsAdvert()).ToList();
										break;

									default:
										feed = Instance.Data.GetFeed(User);
										feed.BackgroundUrl = UserFeedBackgroundUrl;
										break;
								}

								DrawFeed(Instance.Data.GetFeed(MainFeedId), GetPage(0), canAddNewPost: true);
								DrawFeed(feed, GetPage(1), offset: 0.3f, canAddNewPost: true);
								DrawServerViewerBanner();
								DrawNotificationTray();
								break;
						}
					}

					if (isBackground)
					{
						IsGlobalBackground = isBackground;

						CuiHelper.AddUi(Player, Container);
						if (PanelType == PanelTypes.DirectMessages && User.Configuration.PerformanceMode) DrawChatBalloonMessages();

						if (User.Configuration.PerformanceMode) ServerMgr.Instance.Invoke(() => { onDraw?.Invoke(); }, MainFadeout * 1.5f);
						ServerMgr.Instance.Invoke(() => { CloseCover(); IsCooledDown = false; }, MainFadeout * 1.5f);
					}
					else ServerMgr.Instance.Invoke(() =>
					{
						IsGlobalBackground = isBackground;

						CuiHelper.AddUi(Player, Container);
						if (PanelType == PanelTypes.DirectMessages && !User.Configuration.PerformanceMode) DrawChatBalloonMessages();

						onDraw?.Invoke();
						ServerMgr.Instance.Invoke(() => { CloseCover(); IsCooledDown = false; }, MainFadeout * 1.5f);
					}, MainFadeout * 1.5f);
				}
				catch (Exception ex)
				{
					Instance.Log($"{ex}");
				}
			}
			public void Draw(PanelTypes panelType, bool isBackground = false, Action onDraw = null)
			{
				if (User.Configuration.PerformanceMode) isBackground = true;

				PanelType = panelType;
				Draw(isBackground, onDraw);
			}
			public void DrawSplash(bool isBackground = false)
			{
				CloseSplash();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawSplash(isBackground: true);

				var container = new CuiElementContainer();
				var mainBackground = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.25 : 0.6)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, "Overlay", !isBackground ? SplashCUI : SplashGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, mainBackground);
				var background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.02", AnchorMax = "0.99 0.975" }, CursorEnabled = true }, ratioBackground);

				var stretch = Instance.Config.Features.GetHolidayMode() != RootConfig.FeaturesConfig.HolidayModes.None;
				container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(RusterNetworkLogo), new CuiRectTransformComponent { AnchorMin = $"{(stretch ? 0.32 : 0.35)} 0.41", AnchorMax = $"{(stretch ? 0.68 : 0.65)} 0.585" } } });
				container.Add(new CuiLabel { Text = { Text = $"Logging in...", FontSize = 12, Font = DefaultFont, Align = TextAnchor.UpperCenter, Color = "1 1 1 0.5" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 0.4" } }, background);

				CuiHelper.AddUi(Player, container);

				if (isBackground && Instance.Config.Sounds.PlayStartup)
				{
					ServerMgr.Instance.Invoke(() =>
					{
						SendEffectTo(Player, effect: "assets/prefabs/misc/blueprintbase/effects/blueprint_read.prefab");
					}, 1f);
				}
			}
			public void DrawOverlays()
			{
				switch (OverlayPanelType)
				{
					case OverlayPanelTypes.Profile:
						DrawProfile(Instance.Data.GetUser(CurrentUserId, ServerViewer));
						break;

					case OverlayPanelTypes.MessageReaction:
						DrawMessageReaction();
						break;

					default:

						break;
				}
			}
			public void DrawCustom(ulong feed1 = 0, ulong feed2 = 0)
			{
				MainFeedId = 5;
				CustomFeed1 = feed1;
				CustomFeed2 = feed2;
				Draw(PanelTypes.CustomFeeds);
			}
			public void DrawCustom(RusterFeed feed1 = null, RusterFeed feed2 = null)
			{
				DrawCustom(feed1 == null ? 0 : feed1.Id, feed2 == null ? 0 : feed2.Id);
			}

			#endregion

			private void DrawUser()
			{
				var background = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0 0", AnchorMax = "0.2 1" }, CursorEnabled = true }, Background);
				Container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(MainFeedId == MarketplaceFeedId ? RusterMarketplace2Logo : RusterLogo), new CuiRectTransformComponent { AnchorMin = "0 0.9", AnchorMax = "1 1" } } });

				var userBg = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.91" }, CursorEnabled = true }, background);
				if (User.IsVerified())
				{
					Container.Add(new CuiElement { Parent = userBg, Components = { Death.GetRawImage(VerifiedTickUrl), new CuiRectTransformComponent { AnchorMin = $"0.2 0.78", AnchorMax = "0.25 0.87" } } });
				}

				Container.Add(new CuiElement { Parent = userBg, Components = { Death.GetRawImage(User.GetAvatar()), new CuiRectTransformComponent { AnchorMin = "0.05 0.675", AnchorMax = "0.18 0.9" } } });
				if (User.HasFrame()) Container.Add(new CuiElement { Parent = userBg, Components = { Death.GetRawImage(User.GetFrame()), new CuiRectTransformComponent { AnchorMin = "0.05 0.675", AnchorMax = "0.18 0.9", OffsetMin = "-5 -5", OffsetMax = "5 5" } } });
				Container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.05 0.675", AnchorMax = "0.18 0.9" }, Text = { Color = "0 0 0 0" } }, userBg);
				Container.Add(new CuiLabel { Text = { Text = $"<b>{User.GetDisplayName(observer: User)}</b>", FontSize = 20, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"{(User.IsVerified() ? 0.265 : 0.2)} 0", AnchorMax = $"1 0.9" } }, userBg);

				Container.Add(new CuiLabel { Text = { Text = $"{User.GetUsername(observer: User)}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.2 0", AnchorMax = $"1 0.75" } }, userBg);
				Container.Add(new CuiButton { Button = { Command = $"{CloseCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.89 0.77", AnchorMax = $"0.96 0.9" }, Text = { Text = $"X", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, userBg);
				var languageButton = Container.Add(new CuiButton { Button = { Command = $"{LanguageDialogCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.89 0.77", AnchorMax = $"0.96 0.9", OffsetMin = "-22 0", OffsetMax = "-22 0" }, Text = { Text = $"", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, userBg);
				var language = Instance.Config.Localisation.GetLanguage(GetLanguage());
				if (language != null) Container.Add(new CuiElement { Parent = languageButton, Components = { Death.GetRawImage(language.FlagUrl), new CuiRectTransformComponent { AnchorMin = $"0.025 0.05", AnchorMax = "0.985 0.95" } } });

				var buttons1 = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.05 0.29", AnchorMax = "0.95 0.43", OffsetMin = "0 25", OffsetMax = "0 25" }, CursorEnabled = true }, userBg);
				var unreadConversations = User.GetConversations().Where(x => x.HasUnreadConversations(User));
				var count = unreadConversations.Count();
				var unreadMessages = unreadConversations.Any() ? $" — {count.Plural(GetPhrase("unreadconvos", count.ToString("n0")), GetPhrase("unreadconvos_pl", count.ToString("n0")))}" : "";
				Container.Add(new CuiButton { Button = { Command = $"{MainDMsCmd} {User.Id}", Color = PanelType == PanelTypes.DirectMessages ? EwllowButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.8825 1" }, Text = { Text = $"{GetPhrase("directmessages")}{unreadMessages}", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = PanelType == PanelTypes.DirectMessages ? "0 0 0 1" : "1 1 1 0.5" } }, buttons1);
				var settingsButton = Container.Add(new CuiButton { Button = { Command = $"{OpenUserSettingsCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.9 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = PanelType == PanelTypes.DirectMessages ? "0 0 0 1" : "1 1 1 0.5" } }, buttons1);
				Container.Add(new CuiElement { Parent = settingsButton, Components = { Death.GetRawImage(GearUrl, color: "1 1 1 0.5"), new CuiRectTransformComponent { AnchorMin = $"0.225 0.215", AnchorMax = "0.775 0.785" } } });

				var buttons2 = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.95 0.25", OffsetMin = "0 25", OffsetMax = "0 25" }, CursorEnabled = true }, userBg);
				Container.Add(new CuiButton { Button = { Command = $"{MainFeedChangeCmd} {User.Id} 0", Color = MainFeedId == 0 ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"{(Instance.Config.Features.EnableMarketplace ? 0.45 : 0.8825)} 1" }, Text = { Text = GetPhrase("community"), FontSize = 13, Align = TextAnchor.MiddleCenter, Color = MainFeedId == 0 ? "0 0 0 1" : "1 1 1 0.5" } }, buttons2);
				if (Instance.Config.Features.EnableMarketplace) Container.Add(new CuiButton { Button = { Command = $"{MainFeedChangeCmd} {User.Id} 1", Color = MainFeedId == 1 ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0.47 0", AnchorMax = $"0.8825 1" }, Text = { Text = GetPhrase("marketplace"), FontSize = 13, Align = TextAnchor.MiddleCenter, Color = MainFeedId == 1 ? "0 0 0 1" : "1 1 1 0.5" } }, buttons2);
				var settings2Button = Container.Add(new CuiButton { Button = { Command = $"{OpenStoreCmd} {User.Id}", Color = PanelType == PanelTypes.CustomFeeds && (CustomFeed1 == UserStoreFeedId || CustomFeed2 == UserStoreFeedId) ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0.9 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = PanelType == PanelTypes.DirectMessages ? "0 0 0 1" : "1 1 1 0.5" } }, buttons2);
				Container.Add(new CuiElement { Parent = settings2Button, Components = { Death.GetRawImage(ShoppingCartUrl, color: PanelType == PanelTypes.CustomFeeds && (CustomFeed1 == UserStoreFeedId || CustomFeed2 == UserStoreFeedId) ? "0 0 0 1" : "1 1 1 0.5"), new CuiRectTransformComponent { AnchorMin = $"0.175 0.195", AnchorMax = "0.8 0.815" } } });

				// var buttons3 = Container.Add ( new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.05 0.48", AnchorMax = "0.95 0.61" }, CursorEnabled = true }, userBg );
				// Container.Add ( new CuiLabel { Text = { Text = $"{GetPhrase ( "notifications" )}:", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, buttons3 );
				// Container.Add ( new CuiButton { Button = { Command = $"{ConfigFriendsNotificationsCmd} {User.Id}", Color = User.Configuration.FriendsNotifications ? CloseButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0.24 0", AnchorMax = $"0.48 1" }, Text = { Text = $"{GetPhrase ( "friends" )}", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = User.Configuration.FriendsNotifications ? "0 0 0 1" : "1 1 1 0.5" } }, buttons3 );
				// Container.Add ( new CuiButton { Button = { Command = $"{ConfigPushNotificationsCmd} {User.Id}", Color = User.Configuration.PushNotifications ? CloseButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0.5 0", AnchorMax = $"0.735 1" }, Text = { Text = $"{GetPhrase ( "push" )}", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = User.Configuration.PushNotifications ? "0 0 0 1" : "1 1 1 0.5" } }, buttons3 );
				// Container.Add ( new CuiButton { Button = { Command = $"{ConfigRustPlusNotificationsCmd} {User.Id}", Color = User.Configuration.RustPlusNotifications ? CloseButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0.755 0", AnchorMax = $"1 1" }, Text = { Text = $"{GetPhrase ( "rustplus" )}", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = User.Configuration.RustPlusNotifications ? "0 0 0 1" : "1 1 1 0.5" } }, buttons3 );

				DrawUsers(background);
			}
			private void DrawAudioPlayer(string parent)
			{
				var isPlaying = AudioPlayer.IsPlaying();
				var feed = AudioPlayer.PlayedPost?.GetFeed();

				var top = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0.6", }, RectTransform = { AnchorMin = "0.01 0.525", AnchorMax = "0.99 0.98" }, CursorEnabled = true }, parent);
				{
					var text = isPlaying ? $"<size=8>{GetPhrase("fm_nowplaying").ToUpper()}</size>\n<b>{AudioPlayer.GetTrackName()}</b>" : $"<size=8>{GetPhrase("fm_stopped").ToUpper()}</size>{(AudioPlayer.PlayedPost == null ? "" : $"\n<b>{AudioPlayer.GetTrackName()}</b>")}";

					Container.Add(new CuiElement { Parent = top, Components = { Death.GetRawImage(isPlaying ? StopButtonIconUrl : PlayButtonIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.05 0.225", AnchorMax = "0.115 0.775" } } });
					Container.Add(new CuiLabel { Text = { Text = text, FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.15 0", AnchorMax = $"1 1" } }, top);
					if (AudioPlayer.PlayedPost != null) Container.Add(new CuiButton { Button = { Command = $"{(!isPlaying ? PlayPostCmd : StopPostCmd)} {User.Id} {AudioPlayer.PlayedPost?.Id} false", Color = "0 0 0 0" }, Text = { Text = string.Empty }, RectTransform = { AnchorMin = $"0.05 0.225", AnchorMax = "0.115 0.775" } }, top);
				}

				var bottom = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0.3", }, RectTransform = { AnchorMin = "0.01 0.02", AnchorMax = "0.99 0.475" }, CursorEnabled = true }, parent);
				{
					var postCreator = AudioPlayer.PlayedPost?.GetUser();
					Container.Add(new CuiButton { Button = { Command = AudioPlayer.PlayedPost == null ? "" : $"{FullPostCmd} {User.Id} {feed?.Id} {AudioPlayer.PlayedPost.Id}", Color = AudioPlayer.PlayedPost == null ? ArrowButtonColor : NormalButtonColor }, Text = { Text = GetPhrase("fm_openpost"), Color = AudioPlayer.PlayedPost == null ? "1 1 1 1" : "0 0 0 1", Align = TextAnchor.MiddleCenter, FontSize = 8 }, RectTransform = { AnchorMin = $"0.05 0.225", AnchorMax = "0.25 0.775" } }, bottom);
					Container.Add(new CuiButton { Button = { Command = AudioPlayer.PlayedPost == null ? "" : $"{ProfileCmd} {User.Id} {AudioPlayer.PlayedPost.UserId}", Color = AudioPlayer.PlayedPost == null ? ArrowButtonColor : NormalButtonColor }, Text = { Text = GetPhrase("fm_viewuser"), Color = AudioPlayer.PlayedPost == null ? "1 1 1 1" : "0 0 0 1", Align = TextAnchor.MiddleCenter, FontSize = 8 }, RectTransform = { AnchorMin = $"0.275 0.225", AnchorMax = "0.46 0.775" } }, bottom);
					if (postCreator != null && postCreator != User && postCreator.IsFriends(User.Id)) Container.Add(new CuiButton { Button = { Command = AudioPlayer.PlayedPost == null ? "" : $"{DMCmd} {User.Id} {AudioPlayer.PlayedPost.UserId}", Color = AudioPlayer.PlayedPost == null ? ArrowButtonColor : NormalButtonColor }, Text = { Text = GetPhrase("fm_dmuser"), Color = AudioPlayer.PlayedPost == null ? "1 1 1 1" : "0 0 0 1", Align = TextAnchor.MiddleCenter, FontSize = 8 }, RectTransform = { AnchorMin = $"0.665 0.225", AnchorMax = "0.82 0.775" } }, bottom);
					Container.Add(new CuiButton { Button = { Command = AudioPlayer.PlayedPost == null ? "" : $"{StopPostCmd} {User.Id} true", Color = AudioPlayer.PlayedPost == null ? ArrowButtonColor : CloseButtonColor }, Text = { Text = "Stop", Color = AudioPlayer.PlayedPost == null ? "1 1 1 1" : "0 0 0 1", Align = TextAnchor.MiddleCenter, FontSize = 8 }, RectTransform = { AnchorMin = $"0.485 0.225", AnchorMax = "0.64 0.775" } }, bottom);
					var pinButton = Container.Add(new CuiButton { Button = { Command = $"{PinToggleCmd} {User.Id}", Color = !User.Configuration.PinAudioPlayer ? ArrowButtonColor : NormalButtonColor }, Text = { Text = string.Empty, FontSize = 8 }, RectTransform = { AnchorMin = $"0.875 0.225", AnchorMax = "0.95 0.775" } }, bottom);
					Container.Add(new CuiElement { Parent = pinButton, Components = { Death.GetRawImage(PinButtonIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.2 0.25", AnchorMax = "0.75 0.75" } } });
				}
			}
			public void DrawStoriesStripe(string parent)
			{
				var stripe = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.05 0.025", AnchorMax = "0.95 0.975" }, CursorEnabled = true }, parent);
				var buttonOpacity = 0.7;
				var startOffset = 0.05;
				var anchorMin = 0.0 + startOffset;
				var anchorMax = 0.11 + startOffset;
				var spacing = 0.13;

				var stories = ServerViewer.IsViewing() ? ServerViewer.Stories : Instance.Data.Stories.ToArray();
				var page = GetPage(70);
				page.TotalPages = (int)Math.Ceiling((double)stories.Length / StoriesPerPage - 1);

				var pageStories = stories.Skip(StoriesPerPage * page.CurrentPage).Take(StoriesPerPage).ToArray();
				page.Check();

				for (int i = 0; i < pageStories.Length; i++)
				{
					var story = pageStories[i];
					var user = Instance.Data.GetUser(story.UserId, ServerViewer);
					var button = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"{anchorMin} 0", AnchorMax = $"{anchorMax} 1", OffsetMin = "0 -2.5", OffsetMax = "0 -2.5" }, CursorEnabled = true }, stripe);
					var postTime = new DateTime(story.Ticks);
					var hoursLeft = 24 - (DateTime.Now - postTime).TotalHours;

					Container.Add(new CuiElement { Parent = button, Components = { Death.GetRawImage(user.GetAvatar(), color: "1 1 1 0.7"), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
					Container.Add(new CuiElement { Parent = button, Components = { Death.GetRawImage(EclipseUrl, color: "0.2 0.2 0.2 1"), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
					Container.Add(new CuiElement
					{
						Parent = button,
						Components = {
							new CuiTextComponent { Text = $"<b>{hoursLeft:n0}</b>h", FontSize = 10, Color = "1 1 1 1", Font = DefaultFont, Align = TextAnchor.MiddleCenter  },
							new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
							new CuiOutlineComponent { Distance = "0.25 0.25", Color = "0 0 0 1" } }
					});

					Container.Add(new CuiButton { Button = { Command = $"{OpenStoryCmd} {User.Id} {story.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = "", Color = "0 0 0 0" } }, button);

					anchorMin += spacing;
					anchorMax += spacing;
				}

				if (stories.Length == 0) Container.Add(new CuiLabel { Text = { Text = GetPhrase("nostoriesposted"), FontSize = 8, Color = "1 1 1 0.25", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, stripe);

				if (page.TotalPages >= 1)
				{
					Container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.075 0.49" }, Text = { Text = $"◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = $"1 1 1 {buttonOpacity}" } }, parent);
					Container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0.51", AnchorMax = $"0.075 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = $"1 1 1 {buttonOpacity}" } }, parent);
					Container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.925 0", AnchorMax = $"1 0.49" }, Text = { Text = $"▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = $"1 1 1 {buttonOpacity}" } }, parent);
					Container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.925 0.51", AnchorMax = $"1 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = $"1 1 1 {buttonOpacity}" } }, parent);
				}
			}
			public void DrawStory(RusterStory story)
			{
				CloseStory();

				var drawAudioPlayer = User.Configuration.PinAudioPlayer || AudioPlayer.IsPlaying() || AudioPlayer.PlayedPost != null;
				var author = story.GetUser(ServerViewer);
				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 0" }, RectTransform = { AnchorMin = "0 0.175", AnchorMax = "1 1", OffsetMin = $"0 {(!drawAudioPlayer ? -80 : 0)}", OffsetMax = $"0 {(!drawAudioPlayer ? -80 : 0)}" }, CursorEnabled = true }, "Overlay", StoryDialogCUI);
				container.Add(new CuiButton { Button = { Command = $"{CloseStoryCmd} {User.Id}", Color = "0 0 0 0" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, background);

				var views = story.GetViewCount();
				var frame = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 1" }, RectTransform = { AnchorMin = "0.015 0", AnchorMax = "0.195 0.3" } }, background);
				container.Add(new CuiElement { Parent = frame, Components = { Death.GetRawImage(story.PhotoUrl, color: "1 1 1 0.9"), new CuiRectTransformComponent { AnchorMin = "0.025 0.35", AnchorMax = "0.965 0.95" } } });
				container.Add(new CuiLabel { Text = { Text = $"<size=8><b>{views:n0} {views.Plural("view", "views")}</b> — {User.GetTimeSpan(story.Ticks, User.Id)}</size>\n{$"<i>{(string.IsNullOrEmpty(story.PhotoTag) ? "No tag set." : story.PhotoTag)}</i>".Replace("\n", " ")}", FontSize = 10, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"0.965 0.315" } }, frame);
				container.Add(new CuiLabel { Text = { Text = $"{author.GetUsername(observer: User)}", FontSize = 8, Font = DefaultFont, Align = TextAnchor.LowerRight, Color = "1 1 1 0.15" }, RectTransform = { AnchorMin = $"0.025 0.05", AnchorMax = $"0.965 0.2" } }, frame);
				container.Add(new CuiButton { Button = { Command = $"{OpenPictureViewerCmd} {User.Id} {story.PhotoUrl}", Color = "0 0 0 0" }, Text = { Text = string.Empty, Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.025 0.35", AnchorMax = "0.965 0.95" } }, frame);

				if (User == author || User.IsAdmin() || User.IsModerator())
					container.Add(new CuiButton { Button = { Command = $"{DeleteStoryCmd} {User.Id} {story.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = $"0.025 0.025", AnchorMax = $"0.075 0.1" }, Text = { Text = "X", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, frame);

				CuiHelper.AddUi(Player, container);
			}
			private void DrawDirectMessages()
			{
				LockPlayer();

				var conversations = User.GetConversations().OrderByDescending(x => x.Messages.FirstOrDefault()?.Ticks);
				var background = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.575", }, RectTransform = { AnchorMin = $"0.21 0", AnchorMax = $"0.8 1" }, CursorEnabled = true }, Background);
				var selectedConversation = Instance.Data.GetConversation(ConversationId);
				var chatBackground = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.3 0", AnchorMax = $"1 1" }, CursorEnabled = true }, background);
				{
					if (selectedConversation != null)
					{
						var page = GetPage(56);
						var chatInput = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.575", }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 0.04" }, CursorEnabled = true }, chatBackground);
						var userList = selectedConversation?.Users.Where(x => x != User.Id).ToArray();
						var otherUser = Instance.Data.GetUser(userList.FirstOrDefault());

						if (selectedConversation.IsLocked)
						{
							Container.Add(new CuiLabel { Text = { Text = GetPhrase("lockedgroup", otherUser.GetDisplayName(observer: User)), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, chatInput);
						}
						else if (userList.Length > 0 && User.HasBlockedCommunication(otherUser.Id))
						{
							Container.Add(new CuiLabel { Text = { Text = GetPhrase("blockedcommunication", otherUser.GetDisplayName(observer: User)), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, chatInput);
						}
						else if (!userList.Any(x => Instance.Data.GetUser(x).IsBot) && userList.Length > 0 && Instance.Config.DMs.MustBeFriendsToDM && !User.IsFriends(otherUser.Id) && selectedConversation.ConversationType != RusterConversation.ConversationTypes.Team && !(User.IsAdmin() || User.IsModerator()))
						{
							Container.Add(new CuiLabel { Text = { Text = GetPhrase("mustbefriends"), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, chatInput);
						}
						else
						{
							Container.Add(new CuiElement
							{
								Parent = chatInput,
								Components =
								{
									new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{ConversationMessageChangeCmd} {User.Id} ", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 75 },
									new CuiRectTransformComponent { AnchorMin = "0.015 0", AnchorMax = "1 1" }
								}
							});

							Container.Add(new CuiButton { Button = { Command = $"{NewPostAddCassetteCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.7 0", AnchorMax = $"0.8 1" }, Text = { Text = "Send\nCassette", FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, chatInput);
							Container.Add(new CuiButton { Button = { Command = $"{NewPostAddPictureCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.8 0", AnchorMax = $"0.9 1" }, Text = { Text = "Send\nPhoto", FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, chatInput);
							Container.Add(new CuiButton { Button = { Command = $"{ConversationSendLocationCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.9 0", AnchorMax = $"1 1" }, Text = { Text = GetPhrase("sharelocation"), FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, chatInput);
						}
					}
				}

				var listBackground = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.85", }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.3 1" }, CursorEnabled = true }, background);
				{
					var page = GetPage(55);

					Container.Add(new CuiLabel { Text = { Text = $"<b>{GetPhrase("directmessages")}</b> {conversations.Count():n0}", FontSize = 20, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.05 0.925", AnchorMax = $"1 0.99" } }, listBackground);

					Container.Add(new CuiPanel { Image = { Color = User.Configuration.DMNotifications ? NormalButtonColor : ArrowButtonColor, }, RectTransform = { AnchorMin = $"0.9 0.925", AnchorMax = $"0.995 0.95" }, CursorEnabled = true }, listBackground);
					Container.Add(new CuiElement { Parent = listBackground, Components = { Death.GetRawImage(NotifyUrl), new CuiRectTransformComponent { AnchorMin = $"0.925 0.93", AnchorMax = $"0.975 0.945" } } });

					var spacing = 0.0675f;
					var anchorMin = 0.86f;
					var anchorMax = 0.9225f;
					var conversationsCount = conversations.Count();
					page.TotalPages = (int)Math.Ceiling((double)conversationsCount / ConversationsPerPage - 1);

					for (int i = 0; i < page.TotalPages + 1; i++)
					{
						if (selectedConversation != null && conversations.Skip(ConversationsPerPage * i).Take(ConversationsPerPage).Contains(selectedConversation))
						{
							page.CurrentPage = i;
							break;
						}
					}

					var pageConversations = conversations.Skip(ConversationsPerPage * page.CurrentPage).Take(ConversationsPerPage).ToArray();
					page.Check();

					for (int i = 0; i < pageConversations.Count(); i++)
					{
						var conversation = pageConversations[i];
						DrawDirectMessageUser(conversation, conversation.Id, listBackground, anchorMin, anchorMax);
						anchorMin -= spacing;
						anchorMax -= spacing;
					}

					if (page.TotalPages > -1)
					{
						var buttons = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.025 0.01", AnchorMax = $"0.85 0.035" }, CursorEnabled = true }, listBackground);

						Container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.65 0", AnchorMax = $"1 1" } }, buttons);

						var inputPanel = Container.Add(new CuiPanel { Image = { Color = DeselectedButtonColor, }, RectTransform = { AnchorMin = $"0.5015 0", AnchorMax = $"0.61 1" }, CursorEnabled = true }, buttons);
						Container.Add(new CuiElement
						{
							Parent = inputPanel,
							Components =
							{
								new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{SetPageCmd} {User.Id} {page.Id} ", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", CharsLimit = 4 },
								new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
							}
						});
					}
					else
					{
						Container.Add(new CuiLabel { Text = { Text = GetPhrase("noconvoyet"), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, listBackground);
					}

					Container.Add(new CuiButton { Button = { Command = $"{NewGroupCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.025 0.925", AnchorMax = $"0.275 0.95" }, Text = { Text = "<b>+</b><size=8> New Group</size>", Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, listBackground);
					Container.Add(new CuiButton { Button = { Command = $"{ConfigDMNotificationsCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.9 0.925", AnchorMax = $"0.995 0.95" }, Text = { Color = "0 0 0 0" } }, listBackground);
				}
			}
			private void DrawDirectMessageUser(RusterConversation conversation, int id, string parent, float anchorMin, float anchorMax)
			{
				var isSelected = id == ConversationId;
				var userList = conversation.ViewerList.Where(x => x != User.Id).ToArray();
				var otherUser = Instance.Data.GetUser(userList.FirstOrDefault());
				var users = userList.Select((x) =>
				{
					var user = Instance.Data.GetUser(x);
					return $"<b>{user.GetDisplayName(false, observer: User)}</b>";
				}).ToArray();
				var background = Container.Add(new CuiPanel { Image = { Color = isSelected ? NormalButtonColor : "0.3 0.3 0.3 0.4", }, RectTransform = { AnchorMin = $"0.025 {anchorMin}", AnchorMax = $"1 {anchorMax}" }, CursorEnabled = true }, parent);

				var latestMessage = conversation.Messages.FirstOrDefault();
				var hasUnreadConversations = conversation.HasUnreadConversations(User);
				var boldStart = hasUnreadConversations ? "<i><b>" : "";
				var boldEnd = hasUnreadConversations ? "</b></i>" : "";
				var isGroupConversation = conversation.ConversationType == RusterConversation.ConversationTypes.Group || userList.Length > 1 || conversation.ConversationType == RusterConversation.ConversationTypes.Team;
				var textXoffset = !isGroupConversation ? 0.175f : 0.075f;
				if (!isGroupConversation)
				{
					Container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(otherUser.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"0.025 0.175", AnchorMax = "0.135 0.75" } } });
					if (otherUser.HasFrame()) Container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(otherUser.GetFrame()), new CuiRectTransformComponent { AnchorMin = $"0.025 0.175", AnchorMax = "0.135 0.75", OffsetMin = "-5 -5", OffsetMax = "5 5" } } });
				}
				var title = !isGroupConversation ? $"<b>{otherUser.GetDisplayName(false, observer: User)}</b>" : conversation.ConversationType == RusterConversation.ConversationTypes.Team ? GetPhrase("teamchat") : string.IsNullOrEmpty(conversation.CustomTitle) ? GetPhrase("conversation") : conversation.CustomTitle;
				Container.Add(new CuiLabel { Text = { Text = $"{title}{(hasUnreadConversations ? $" <size=10>{conversation.UnreadMessageCount(User):n0}</size>" : "")}", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft, Color = isSelected ? "0 0 0 1" : (conversation.HasUnreadConversations(User) ? NormalButtonColor : "1 1 1 1") }, RectTransform = { AnchorMin = $"{textXoffset} 0", AnchorMax = $"1 0.8" } }, background);
				Container.Add(new CuiLabel { Text = { Text = latestMessage == null ? GetPhrase("norecentmessages") : $"{boldStart}{latestMessage.GetSender().GetDisplayName(false, observer: User)}{boldEnd}: <i>{latestMessage.Message.Replace("\n", "").EscapeRichText().Truncate(20, "...")}</i>", FontSize = 10, Color = isSelected ? "0 0 0 0.75" : "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"{textXoffset} 0", AnchorMax = $"1 0.45" } }, background);

				if (isGroupConversation && userList.Length > 1)
				{
					Container.Add(new CuiLabel { Text = { Text = $"{userList.Length:n0} users", FontSize = 10, Color = isSelected ? "0 0 0 1" : "1 1 1 1", Font = DefaultFont, Align = TextAnchor.UpperRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.975 0.85" } }, background);
				}

				if (!isSelected) Container.Add(new CuiButton { Button = { Command = $"{ChangeConversationCmd} {User.Id} {id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = $"", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
				if (conversation.CanDelete) Container.Add(new CuiButton { Button = { Command = $"{ConversationDeleteCmd} {User.Id} {id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.83 0.15", AnchorMax = $"0.98 0.47" }, Text = { Text = $"DELETE", FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, background);
				if (!isGroupConversation) Container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {otherUser.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.025 0.175", AnchorMax = "0.135 0.75" }, Text = { Text = $"", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
			}
			private void DrawDirectMessageBalloon(CuiElementContainer container, RusterConversation.RusterDirectMessage message, bool isInitial, string parent, float anchorMin, float anchorMax)
			{
				var isMe = message.SenderId == User.Id;
				var sender = message.GetSender();
				var timeAgo = DateTime.Now - new DateTime(message.Ticks);
				var isPhotograph = !string.IsNullOrEmpty(message.PhotographUrl);
				var isAudio = message.CassetteId != 0;
				var isPlaying = AudioPlayer?.PlayedPost?.Id == message.Id;

				var background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"{(isMe ? 0.625 : 0.025)} {anchorMin}", AnchorMax = $"{(isMe ? 0.975 : 0.4)} {anchorMax}" }, CursorEnabled = true }, parent);
				var selectedConversation = Instance.Data.GetConversation(ConversationId);

				if (isPhotograph)
				{
					container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(message.PhotographUrl), new CuiRectTransformComponent { AnchorMin = $"0.03 0.12", AnchorMax = "0.94 0.895", OffsetMin = $"{(isMe ? 0 : 5)} 0", OffsetMax = $"{(isMe ? 0 : 5)} 0" } } });
				}

				container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(isMe ? (isInitial ? SentInitialBalloonUrl : SentFurtherBalloonUrl) : (isInitial ? SenderInitialBalloonUrl : SenderFurtherBalloonUrl), color: $"0.1 0.1 0.1 {(message.IsSystemMessage ? 0.4 : 0.7)}"), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = "1 1" } } });
				container.Add(new CuiLabel { Text = { Text = $"<b>{sender.GetDisplayName(observer: User)}</b>", FontSize = 13, Font = DefaultFont, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" }, RectTransform = { AnchorMin = $"{(isMe ? 0.15 : 0.175)} 0.05", AnchorMax = $"{(isMe ? 0.9 : 1)} 0.9" } }, background);
				container.Add(new CuiLabel { Text = { Text = $"{(message.IsSystemMessage ? "<i>" : "")}{message.Message.Truncate(125, "...")}{(message.IsSystemMessage ? "</i>" : "")}", FontSize = 9, Font = DefaultFont, Align = TextAnchor.UpperLeft, Color = $"1 1 1 {(message.IsSystemMessage ? 0.5 : 1)}" }, RectTransform = { AnchorMin = $"{(isMe ? 0.05 : 0.075)} 0.02", AnchorMax = $"{(isMe ? 0.95 : 0.95)} 0.65" } }, background);

				container.Add(new CuiLabel { Text = { Text = $"{User.GetTimeSpan(message.Ticks, User.Id)}", FontSize = 8, Font = DefaultFont, Align = TextAnchor.LowerLeft, Color = "0.7 0.7 0.7 0.7" }, RectTransform = { AnchorMin = $"{(isMe ? 0.05 : 0.075)} 0.075", AnchorMax = $"{(isMe ? 0.9 : 1)} 0.7" } }, background);
				if (message.SenderId == User.Id) container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(message.Status == RusterConversation.RusterDirectMessage.StatusTypes.Read ? ReadCheckUrl : SentCheckUrl), new CuiRectTransformComponent { AnchorMin = $"{(isMe ? 0.875 : 0.9)} 0.03", AnchorMax = $"{(isMe ? 0.95 : 0.97)} 0.25" } } });
				container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(sender.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"{(isMe ? 0.05 : 0.075)} 0.7", AnchorMax = $"{(isMe ? 0.125 : 0.14)} 0.9" } } });
				if (sender.HasFrame()) container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(sender.GetFrame()), new CuiRectTransformComponent { AnchorMin = $"{(isMe ? 0.05 : 0.075)} 0.7", AnchorMax = $"{(isMe ? 0.125 : 0.14)} 0.9", OffsetMin = "-3 -3", OffsetMax = "3 3" } } });

				if (isPhotograph)
				{
					container.Add(new CuiButton { Button = { Command = $"{OpenPictureViewerCmd} {User.Id} {message.PhotographUrl}", Color = "0 0 0 0" }, Text = { Text = string.Empty, Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {message.SenderId}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"{(isMe ? 0.05 : 0.075)} 0.7", AnchorMax = $"{(isMe ? 0.125 : 0.14)} 0.9" }, Text = { Text = $"", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
				}
				else if (message.IsPost())
				{
					var post = Instance.Data.GetPost(message.PostId);

					if (post != null) container.Add(new CuiButton { Button = { Command = $"{FullPostCmd} {User.Id} {message.FeedId} {message.PostId}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.05 0.275", AnchorMax = $"0.925 0.625", OffsetMin = isMe ? "0 0" : "5 0", OffsetMax = isMe ? "0 0" : "5 0" }, Text = { Text = $"Open {post.GetUser().GetDisplayName(coloured: false, observer: User)}'s Post", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = isPlaying ? "1 1 1 1" : "0 0 0 1" } }, background);
					else container.Add(new CuiButton { Button = { Command = "", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.05 0.275", AnchorMax = $"0.925 0.625", OffsetMin = isMe ? "0 0" : "5 0", OffsetMax = isMe ? "0 0" : "5 0" }, Text = { Text = $"Post Unavailable", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.8" } }, background);
				}
				else if (message.IsTrade())
				{
					if (!message.IsTradeCompleted() && message.GetTimeSince() <= 60f * 2f && message.SenderId != User.Id) container.Add(new CuiButton { Button = { Command = $"{ConversationTradeCmd} {User.Id} {message.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.05 0.275", AnchorMax = $"0.925 0.625", OffsetMin = isMe ? "0 0" : "5 0", OffsetMax = isMe ? "0 0" : "5 0" }, Text = { Text = $"Open Trade", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = isPlaying ? "1 1 1 1" : "0 0 0 1" } }, background);
					else container.Add(new CuiButton { Button = { Command = "", Color = message.SenderId == User.Id ? "0.2 0.2 0.2 0.4" : ArrowButtonColor }, RectTransform = { AnchorMin = "0.05 0.275", AnchorMax = $"0.925 0.625", OffsetMin = isMe ? "0 0" : "5 0", OffsetMax = isMe ? "0 0" : "5 0" }, Text = { Text = message.SenderId != User.Id && (message.GetTimeSince() > 60f * 2f || message.IsTradeCompleted()) ? "Trade Expired" : (message.GetTimeSince() > 60f * 2f || message.IsTradeCompleted()) ? "Trade Expired" : "Trade Sent", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.8" } }, background);
				}
				else
				{
					container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {message.SenderId}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = $"", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
				}

				if (isAudio) { container.Add(new CuiButton { Button = { Command = $"{(isPlaying ? StopPostCmd : PlayMessageCmd)} {User.Id} {(isPlaying ? "true" : message.Id.ToString())}", Color = isPlaying ? ArrowButtonColor : NormalButtonColor }, RectTransform = { AnchorMin = "0.05 0.275", AnchorMax = $"0.925 0.625" }, Text = { Text = isPlaying ? "Stop" : "Play", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = isPlaying ? "1 1 1 1" : "0 0 0 1" } }, background); }

				if (message.Id != 0 && message.SenderId == User.Id && timeAgo.TotalSeconds <= Instance.Config.DMs.DeleteOwnMessagesCooldown) container.Add(new CuiButton { Button = { Command = $"{ConversationMessageDeleteCmd} {User.Id} {message.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.95 0.85", AnchorMax = $"1 1" }, Text = { Text = $"X", FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, background);

				if (selectedConversation.CanReact && ((message.SenderId == User.Id && !string.IsNullOrEmpty(message.Reaction)) || (message.SenderId != User.Id))) container.Add(new CuiElement { Parent = background, Components = { new CuiRawImageComponent { Url = !string.IsNullOrEmpty(message.Reaction) ? Instance.GetEmojiUrl(message.Reaction) : AddReactionIconUrl, Color = $"1 1 1 1" }, new CuiRectTransformComponent { AnchorMin = $"0 0.79", AnchorMax = $"0.07 1" } } });
				if (message.SenderId != User.Id)
				{
					container.Add(new CuiButton { Button = { Command = $"{ChangeMessageReactionCmd} {User.Id} {message.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0.79", AnchorMax = $"0.07 1" }, Text = { Text = $"", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
				}
			}
			public bool DrawChatBalloonMessages(bool isBackground = false)
			{
				CloseChatBalloonMessages();

				if ((PanelType != PanelTypes.DirectMessages && OverlayPanelType != OverlayPanelTypes.None && ConversationId == 0) || IsBusy) return false;

				if (!isBackground && !User.Configuration.PerformanceMode) DrawChatBalloonMessages(isBackground: true);

				var container = new CuiElementContainer();
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio(User.Configuration.Ratio.Scale(16, 21, 0.3875f, 0.2725f))} 0.06", AnchorMax = $"{User.Configuration.GetMaxRatio(User.Configuration.Ratio.Scale(16, 21, 0.795f, 0.855f))} 0.975" }, CursorEnabled = true }, "Overlay", !isBackground ? ChatBalloonMessagesNoticeCUI : ChatBalloonMessagesNoticeGhostCUI);
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(isBackground ? 0 : Instance.Config.Look.DisableBlur ? 0 : 0.2)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = false }, ratioBackground);
				var selectedConversation = Instance.Data.GetConversation(ConversationId);

				if (selectedConversation == null)
				{
					container.Add(new CuiLabel
					{
						Text = { Text = GetPhrase("noconvoselected"), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter },
						RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }
					}, background);
				}
				else
				{
					var page = GetPage(56);
					var userList = selectedConversation?.Users.Where(x => x != User.Id).ToArray();
					var otherUser = Instance.Data.GetUser(userList.FirstOrDefault());

					var chatList = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0 0.02", AnchorMax = $"1 1" }, CursorEnabled = true }, background);

					var spacing = 0.105f;
					var anchorMin = 0.04f;
					var anchorMax = 0.14f;
					var lastUser = 0UL;

					var pageMessages = selectedConversation.Messages.Skip(DMsPerPage * page.CurrentPage).Take(DMsPerPage).ToArray();
					page.TotalPages = (int)Math.Ceiling((double)selectedConversation.Messages.Count / DMsPerPage - 1);
					page.Check();

					foreach (var message in pageMessages)
					{
						var isInitial = lastUser != message.SenderId;

						if (message.SenderId != User.Id) message.MarkRead();
						DrawDirectMessageBalloon(container, message, isInitial, chatList, anchorMin, anchorMax);
						anchorMin += spacing;
						anchorMax += spacing;

						lastUser = message.SenderId;
					}

					if (page.TotalPages > -1)
					{
						var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.015 0.01", AnchorMax = $"0.5 0.035" }, CursorEnabled = true }, background);

						container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.65 0", AnchorMax = $"1 1" } }, buttons);

						var inputPanel = container.Add(new CuiPanel { Image = { Color = DeselectedButtonColor, }, RectTransform = { AnchorMin = $"0.5015 0", AnchorMax = $"0.61 1" }, CursorEnabled = true }, buttons);
						container.Add(new CuiElement
						{
							Parent = inputPanel,
							Components =
							{
								new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{SetPageCmd} {User.Id} {page.Id} ", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", CharsLimit = 4 },
								new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
							}
						});
					}
					else
					{
						container.Add(new CuiLabel { Text = { Text = GetPhrase("nomessagesinconvo"), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.LowerCenter }, RectTransform = { AnchorMin = $"0 0.05", AnchorMax = $"1 0.1" } }, background);
					}

					if (selectedConversation.Users.Count < 20 && selectedConversation.CanManage) container.Add(new CuiButton { Button = { Command = $"{GroupAddCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.9 0.01", AnchorMax = $"0.99 0.035" }, Text = { Text = "<b>+</b><size=8> Invite</size>", Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);

					userList = null;
					pageMessages = null;
				}

				// User List
				if (selectedConversation != null && selectedConversation.ConversationType == RusterConversation.ConversationTypes.Group)
				{
					var userList = selectedConversation.ViewerList.Where(x => x != User.Id).ToArray();

					var spacing = 0.05;
					var minAnchor = 0.94;
					var maxAnchor = 0.975;

					foreach (var user in userList)
					{
						var userListUser = Instance.Data.GetUser(user);
						var icon = new CuiElement { Parent = background, Components = { Death.GetRawImage(userListUser.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"{minAnchor} 0.95", AnchorMax = $"{maxAnchor} 0.98" } } };
						container.Add(icon);
						container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {user}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, Text = { Text = $"", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, icon.Name);
						if (selectedConversation.IsCreator(User.Id) && selectedConversation.CanManage) container.Add(new CuiButton { Button = { Command = $"{GroupKickCmd} {User.Id} {user}", Color = "0.9 0.3 0.2 1" }, RectTransform = { AnchorMin = $"{minAnchor} 0.935", AnchorMax = $"{maxAnchor} 0.95" }, Text = { Text = $"x", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);

						minAnchor -= spacing;
						maxAnchor -= spacing;
					}
				}

				CuiHelper.AddUi(Player, container);

				return true;
			}

			public void DrawLaunchConfirmation()
			{
				LaunchConfirmationTimer?.Destroy();
				LaunchConfirmationTimer = Instance.timer.In(3.5f, () =>
				{
					if (PendingSlotConfirmation)
					{
						CloseScreenNotice();
						PendingSlotConfirmation = false;

						LaunchConfirmationTimer?.Destroy();
						LaunchConfirmationTimer = null;
					}
				});

				DrawScreenNotice($"<size=13>Re-select the current slot\nto open <b><color=orange>Ruster.NET</color></b>.</size>");
			}

			private void DrawUsers(string parent)
			{
				var drawAudioPlayer = User.Configuration.PinAudioPlayer || AudioPlayer.IsPlaying() || AudioPlayer.PlayedPost != null;
				var backgroundColor = "0.2 0.2 0.2 0.5";
				var background = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.05 0", AnchorMax = "0.95 0.745" }, CursorEnabled = true }, parent);
				var friendRequestsParent = Container.Add(new CuiPanel { Image = { Color = backgroundColor, }, RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 1" }, CursorEnabled = true }, background);
				{
					var page = GetPage(50);
					var requests = Facepunch.Pool.GetList<RusterUser>();
					var sentRequests = User.GetSentFriendRequests();
					requests.AddRange(User.GetReceivedFriendRequests().OrderBy(x => x.GetDisplayName(observer: User)));
					requests.AddRange(sentRequests.OrderBy(x => x.GetDisplayName(observer: User)));

					var pageRequests = requests.Skip(FriendRequestsPerPage * page.CurrentPage).Take(FriendRequestsPerPage).ToArray();
					page.TotalPages = (int)Math.Ceiling((double)requests.Count / FriendRequestsPerPage - 1);
					page.Check();

					Container.Add(new CuiLabel { Text = { Text = $"<b>{GetPhrase("friendrequests")}</b> {requests.Count:n0}", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0.05", AnchorMax = $"0.975 0.95" } }, friendRequestsParent);

					Facepunch.Pool.FreeList(ref requests);

					var spacing = 0.2f;
					var anchorMin = 0.53f;
					var anchorMax = 0.825f;
					foreach (var friend in pageRequests)
					{
						DrawListUser(friend, true, sentRequests.Any(x => x.Id == friend.Id), Container, friendRequestsParent, anchorMin, anchorMax);
						anchorMin -= spacing;
						anchorMax -= spacing;
					}

					if (page.TotalPages > -1)
					{
						var buttons = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.025 0.025", AnchorMax = $"0.85 0.125" }, CursorEnabled = true }, friendRequestsParent);

						Container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
					}
					else
					{
						Container.Add(new CuiLabel { Text = { Text = GetPhrase("nofriendrequests", ConVar.Server.hostname), FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, friendRequestsParent);
					}
				}

				var friendsParent = Container.Add(new CuiPanel { Image = { Color = backgroundColor }, RectTransform = { AnchorMin = $"0 {(drawAudioPlayer ? 0.15 : 0) + 0.085}", AnchorMax = "1 0.69" }, CursorEnabled = true }, background);
				{
					var page = GetPage(51);
					var friends = User.GetFriends().OrderBy(x => x.GetDisplayName(observer: User)).ToArray();
					var friendsList = Facepunch.Pool.GetList<RusterUser>();
					friendsList.AddRange(friends);
					friendsList.AddRange(User.GetBlockedPlayers());

					var countPerPage = FriendsPerPage + (drawAudioPlayer ? 0 : 1);
					var pageRequests = friendsList.Skip(countPerPage * page.CurrentPage).Take(countPerPage).ToArray();
					page.TotalPages = (int)Math.Ceiling((double)friendsList.Count / countPerPage - 1);
					page.Check();

					Container.Add(new CuiLabel { Text = { Text = $"<b>{GetPhrase("friends")}</b> {friends.Length:n0}", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0.025", AnchorMax = $"0.975 0.975" } }, friendsParent);

					Facepunch.Pool.FreeList(ref friendsList);

					var spacing = drawAudioPlayer ? 0.13f : 0.11f;
					var anchorMin = drawAudioPlayer ? 0.72f : 0.76f;
					var anchorMax = drawAudioPlayer ? 0.925f : 0.915f;
					foreach (var friend in pageRequests)
					{
						DrawListUser(friend, false, false, Container, friendsParent, anchorMin, anchorMax);
						anchorMin -= spacing;
						anchorMax -= spacing;
					}

					if (page.TotalPages > -1)
					{
						var buttons = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.025 0.025", AnchorMax = $"0.85 0.075" }, CursorEnabled = true }, friendsParent);

						Container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						Container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
					}
					else
					{
						Container.Add(new CuiLabel { Text = { Text = GetPhrase("nofriends"), FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, friendsParent);
					}

					Container.Add(new CuiButton { Button = { Command = $"{OpenContactsCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.91 0.92", AnchorMax = $"0.975 0.98" }, Text = { Text = "+", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, friendsParent);
				}

				var storiesParent = Container.Add(new CuiPanel { Image = { Color = "0.2 0.2 0.2 1" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.05", OffsetMin = $"0 {(drawAudioPlayer ? 80 : 0)}", OffsetMax = $"0 {(drawAudioPlayer ? 80 : 0)}" }, CursorEnabled = true }, background);
				{
					DrawStoriesStripe(storiesParent);
					if (!ServerViewer.IsViewing() && Instance.HasPermission(Player, StoryPerm, true))
						Container.Add(new CuiButton { Button = { Command = $"{CreateStoryCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = $"0.75 0.13", AnchorMax = "0.785 0.15", OffsetMin = $"0 {(drawAudioPlayer ? 40 : -40)}", OffsetMax = $"0 {(drawAudioPlayer ? 40 : -40)}" }, Text = { Text = $"+", FontSize = 9, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, background);
					Container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(RusterStoriesLogo), new CuiRectTransformComponent { AnchorMin = $"0.8 0.13", AnchorMax = "0.98 0.15", OffsetMin = $"0 {(drawAudioPlayer ? 40 : -40)}", OffsetMax = $"0 {(drawAudioPlayer ? 40 : -40)}" } } });
				}

				if (drawAudioPlayer)
				{
					var feed = AudioPlayer.PlayedPost?.GetFeed();
					var audioPlayerParent = Container.Add(new CuiPanel { Image = { Color = !IsGlobalBackground && AudioPlayer.IsPlaying() ? "0.7 0.85 0.1 0.5" : backgroundColor, }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.14" }, CursorEnabled = true }, background);
					{
						DrawAudioPlayer(audioPlayerParent);
						Container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(RusterFMLogo), new CuiRectTransformComponent { AnchorMin = $"0.765 0.13", AnchorMax = "0.98 0.15", OffsetMin = "0 -7", OffsetMax = "0 -7" } } });
					}
				}
			}
			private void DrawListUser(RusterUser user, bool isFriendRequest, bool isPending, CuiElementContainer container, string parent, float anchorMin, float anchorMax, bool interactible = true, string command = null)
			{
				if (string.IsNullOrEmpty(command)) command = ProfileCmd;

				var background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0 {anchorMin}", AnchorMax = $"1 {anchorMax}" }, CursorEnabled = true }, parent);
				container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(user.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"0.025 0.175", AnchorMax = "0.135 0.75" } } });
				if (user.HasFrame()) container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(user.GetFrame()), new CuiRectTransformComponent { AnchorMin = $"0.025 0.175", AnchorMax = "0.135 0.75", OffsetMin = "-5 -5", OffsetMax = "5 5" } } });

				if (user.IsVerified())
				{
					container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(VerifiedTickUrl), new CuiRectTransformComponent { AnchorMin = $"0.175 0.5", AnchorMax = "0.225 0.75" } } });
				}
				container.Add(new CuiLabel { Text = { Text = $"{(User.HasBlocked(user.Id) ? $"<color=orange>{GetPhrase("blockedtonite")}</color>" : $"<b>{user.GetDisplayName(observer: User)}</b> {user.GetOnlineIcon()}")}", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"{(user.IsVerified() ? 0.235 : 0.175)} 0", AnchorMax = $"1 0.8" } }, background);
				container.Add(new CuiLabel { Text = { Text = $"{user.GetUsername(observer: User)}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.175 0", AnchorMax = $"1 0.45" } }, background);

				if (isFriendRequest)
				{
					if (isPending)
					{
						container.Add(new CuiLabel { Text = { Text = GetPhrase("sent"), FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperRight }, RectTransform = { AnchorMin = "0 0.35", AnchorMax = $"0.95 0.75" } }, background);
						if (interactible) container.Add(new CuiButton { Button = { Command = $"{command} {User.Id} {user.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }, Text = { Text = $"", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
					}
					else
					{
						if (interactible)
						{
							container.Add(new CuiButton { Button = { Command = $"{command} {User.Id} {user.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }, Text = { Text = $"", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);

							container.Add(new CuiButton { Button = { Command = $"{HandleFriendRequestCmd} {User.Id} {user.Id} true false", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.8 0.35", AnchorMax = $"0.8725 0.75" }, Text = { Text = $"<b>+</b>", FontSize = 15, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
							container.Add(new CuiButton { Button = { Command = $"{HandleFriendRequestCmd} {User.Id} {user.Id} false false", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.8725 0.35", AnchorMax = $"0.95 0.75" }, Text = { Text = $"<b>-</b>", FontSize = 15, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
						}
					}
				}
				else
				{
					if (interactible) container.Add(new CuiButton { Button = { Command = $"{command} {User.Id} {user.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }, Text = { Text = $"", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
				}
			}
			private void DrawFeed(CuiElementContainer container, string parent, RusterFeed feed, Page browserFeed, float offset = 0f, bool allowProfilePreviews = true, bool canAddNewPost = false, bool canReply = true)
			{
				if (!feed.ShowReplies) canReply = false;

				var canSee = !User.HasBlockedCommunication(feed.Id);
				var background = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.975", }, RectTransform = { AnchorMin = $"{0.21f + offset} 0", AnchorMax = $"{0.5f + offset} 1" }, CursorEnabled = true }, parent);
				var subBackground = container.Add(new CuiPanel { Image = { Color = feed.GetBackgroundColor(), }, RectTransform = { AnchorMin = "0 0.925", AnchorMax = "1 1" }, CursorEnabled = true }, background);

				if (!string.IsNullOrEmpty(feed.BackgroundUrl)) container.Add(new CuiElement { Parent = subBackground, Components = { Death.GetRawImage(feed.BackgroundUrl, color: "1 1 1 0.65"), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = "1 1" } } });

				container.Add(new CuiLabel { Text = { Text = $"<b>{feed.GetFeedTitle(User)}</b>", Color = feed.DarkMode ? feed.GetDarkTitleColor() : feed.GetTitleColor(), FontSize = 20, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.8" } }, subBackground);
				container.Add(new CuiLabel { Text = { Text = $"{feed.GetFeedType()} Feed — {feed.Posts.Count.Plural(GetPhrase("post_count", feed.Posts.Count.ToString("n0")), GetPhrase("posts_count", feed.Posts.Count.ToString("n0")))}{(feed.GetFeedType() == RusterFeed.FeedTypes.Shop ? $" — {GetPhrase("wallet")}: {Instance.Config.Currency.GetValueName(this, User.Wallet, true)}{(User.Configuration.PaymentMethod != RusterUserConfiguration.PaymentMethods.Wallet ? $"— <b>{Instance.Config.Currency.GetShortname(this)}</b>: {GetPlayerCurrency():n0}" : "")}" : "")}", FontSize = 10, Color = feed.DarkMode ? feed.GetDarkSubtitleColor() : feed.GetSubtitleColor(), Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.4" } }, subBackground);
				if (!canSee) container.Add(new CuiLabel { Text = { Text = GetPhrase("cantviewfeed"), FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, background);

				var originalPosts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				if (feed.AllowPinning) originalPosts.AddRange(feed.Posts.OrderByDescending(x => x.IsPinned)); else originalPosts.AddRange(feed.Posts);

				if (browserFeed.CurrentHashtag != null)
				{
					switch (browserFeed.CurrentHashtag.FilterType)
					{
						case RusterHashtag.FilterTypes.Content:
							originalPosts = originalPosts.Where(x => x.Content.ToLower().Contains($"{browserFeed.CurrentHashtag.Filter?.ToLower()}")).ToList();
							break;

						case RusterHashtag.FilterTypes.ItemName:
							originalPosts = originalPosts.Where(x => x.IsMarketplaceListing() && x.MarketplaceListing.CustomName.ToLower().Contains(browserFeed.CurrentHashtag.Filter?.ToLower())).ToList();
							break;

						case RusterHashtag.FilterTypes.ItemShortname:
							originalPosts = originalPosts.Where(x => x.IsMarketplaceListing() && x.MarketplaceListing.Shortname.ToLower().Contains(browserFeed.CurrentHashtag.Filter?.ToLower())).ToList();
							break;
					}
				}

				if (feed.AllowAdverts && Instance.Config.Features.EnableMarketplace)
				{
					for (int i = 0; i < feed.Posts.Count; i++)
					{
						if (RandomEx.GetRandomInteger(0, AdPossiblities, AdPlacementSeed + i + browserFeed.CurrentPage + browserFeed.Id + feed.Posts.Count) <= AdChance)
						{
							try { originalPosts.Insert(i, Instance.Data.GetRandomAdvert(User)); } catch { }
						}
					}
				}

				if (Instance.Config.Features.EnableMarketplace && Instance.Config.Ads.ShowRusterNETAdvertsInMarketplace && feed.Id == MarketplaceFeedId)
				{
					originalPosts.Insert(0, Instance.Data.GetAdvertsNoticePost(false, User));
					originalPosts.Insert(0, Instance.Data.GetAdvertsNoticePost(true, User));

					if (Instance.Config.Features.EnableFlipbook)
					{
						originalPosts.Add(Instance.Data.GetFlipbookAdvertNoticePost(2, User));
						originalPosts.Add(Instance.Data.GetFlipbookAdvertNoticePost(1, User));
						originalPosts.Add(Instance.Data.GetFlipbookAdvertNoticePost(0, User));
					}
				}

				var posts = !canSee ? new RusterFeed.RusterPost[0] : originalPosts.Skip(PostsPerPage * browserFeed.CurrentPage).Take(PostsPerPage).ToArray();
				browserFeed.TotalPages = !canSee ? 0 : (int)Math.Ceiling((double)originalPosts.Count / PostsPerPage - 1);
				browserFeed.Check();

				var subtracter = 0.175f;
				var postAnchorMin = 0.75f;
				var postAnchorMax = 0.92f;
				var lineMultiplier = 0;
				for (int i = 0; i < posts.Length; i++)
				{
					DrawPost(container, background, feed, posts[i], postAnchorMin, postAnchorMax, ref lineMultiplier, allowProfilePreviews, canReply);

					postAnchorMin -= subtracter + (lineMultiplier * 0.05f);
					postAnchorMax -= subtracter + (lineMultiplier * 0.05f);
				}

				var mask = browserFeed.TotalPages == -1;
				var maskText = GetPhrase("nothinghere");

				if (mask) container.Add(new CuiLabel { Text = { Text = maskText, FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 0.925" } }, background);
				DrawFeedButtons(container, background, browserFeed, feed, canAddNewPost, feed.GetFeedType() == RusterFeed.FeedTypes.Post);
				if (canAddNewPost)
				{
					if (feed.IsLocked) { container.Add(new CuiLabel { Text = { Text = GetPhrase("threadislocked"), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.825 0", AnchorMax = $"0.95 1" } }, subBackground); }
					else container.Add(new CuiButton { Button = { Command = $"{NewPostCmd} {User.Id} {feed.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.825 0", AnchorMax = $"0.95 0.3" }, Text = { Text = !canReply ? GetPhrase("reply") : GetPhrase("newpost"), FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, subBackground);
				}

				if (!feed.DisableFleaMarket && feed.FeedType == RusterFeed.FeedTypes.Shop) container.Add(new CuiButton { Button = { Command = $"{FleaMarketCmd} {User.Id}", Color = EwllowButtonColor }, RectTransform = { AnchorMin = "0.675 0", AnchorMax = $"0.81 0.3" }, Text = { Text = "Flea Market", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, subBackground);

				if (feed.GetFeedType() == RusterFeed.FeedTypes.Shop)
				{
					var basket = Instance.Data.GetGiftBasket(User);
					var spacing = 0.4f;
					container.Add(new CuiButton { Button = { Command = $"{WithdrawCmd} {User.Id}", Color = User.Wallet > 0 ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"{0.45 - spacing} 0.8", AnchorMax = $"{0.583 - spacing} 1" }, Text = { Text = $"<b>{GetPhrase("withdraw")}</b>", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = User.Wallet > 0 ? "0 0 0 1" : "1 1 1 1" } }, subBackground);
					container.Add(new CuiButton { Button = { Command = $"{RestockAllCmd} {User.Id}", Color = User.CanRestockAll() ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"{0.59 - spacing} 0.8", AnchorMax = $"{0.71 - spacing} 1" }, Text = { Text = $"<b>{GetPhrase("restock")}</b>", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = User.CanRestockAll() ? "0 0 0 1" : "1 1 1 1" } }, subBackground);
					container.Add(new CuiButton { Button = { Command = $"{OpenTransactionListCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"{0.59 - spacing} 0.8", AnchorMax = $"{0.76 - spacing} 1", OffsetMin = "47.5 0", OffsetMax = "47.5 0" }, Text = { Text = $"<b>{GetPhrase("transactions")}</b>", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, subBackground);
					container.Add(new CuiButton { Button = { Command = $"{OpenCouponListCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"{0.6 - spacing} 0.8", AnchorMax = $"{0.72 - spacing} 1", OffsetMin = "108 0", OffsetMax = "108 0" }, Text = { Text = $"<b>{GetPhrase("coupons")}</b>", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, subBackground);
					container.Add(new CuiButton { Button = { Command = $"{OpenGiftBasketCmd} {User.Id}", Color = basket.Count > 0 ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"{0.6 - spacing} 0.8", AnchorMax = $"{0.8 - spacing} 1", OffsetMin = "153 0", OffsetMax = "153 0" }, Text = { Text = $"<b>{GetPhrase("giftbasket")}</b> ({basket.Count:n0} {basket.Count.Plural("item", "items")})".ToUpper(), FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = basket.Count > 0 ? "0 0 0 1" : "1 1 1 1" } }, subBackground);
				}

				if (feed.CanFilter)
				{
					var currentFilterHashtag = browserFeed.CurrentHashtag;
					var searchButton = container.Add(new CuiButton { Button = { Command = $"{HashtagFilterCmd} {User.Id} {browserFeed.Id} ", Color = browserFeed.CurrentHashtag != null && browserFeed.IsCustomFilter && browserFeed.CurrentHashtag.IsSame(currentFilterHashtag) ? NormalButtonColor : "0 0 0 0" }, RectTransform = { AnchorMin = "0.9525 0", AnchorMax = $"0.9975 0.3" }, Text = { Text = "", FontSize = 7, Color = "0 0 0 0" } }, subBackground);
					container.Add(new CuiElement { Parent = searchButton, Components = { Death.GetRawImage(MagnifyingGlass), new CuiRectTransformComponent { AnchorMin = $"0.2 0.25", AnchorMax = "0.8 0.825" } } });
				}

				if (feed.ShowHashtags)
				{
					var hashtagSpacing = 0.07f;
					var hashtagAnchorMin = 0.022f;
					var hashtagAnchorMax = 0.024f;
					var hashtags = feed.GetHashtags().Take(6);
					var append = 0f;

					foreach (var hashtag in hashtags)
					{
						var text = hashtag.Filter.Truncate(13, "...", true);
						text = $"#{text}{(hashtag.Instances > 1 ? $" <b>{hashtag.Instances:n0}</b>" : "")}".ToLower();
						var scale = text.Length;
						hashtagAnchorMax += (scale * 0.0035f) + hashtagSpacing + append;

						container.Add(new CuiButton { Button = { Command = $"{ChangeHashtagCmd} {User.Id} {browserFeed.Id} {(int)hashtag.FilterType} {hashtag.Filter}", Color = browserFeed.CurrentHashtag != null && browserFeed.CurrentHashtag.IsSame(hashtag) ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"{hashtagAnchorMin} 0", AnchorMax = $"{hashtagAnchorMax} 0.185" }, Text = { Text = text, FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = browserFeed.CurrentHashtag != null && browserFeed.CurrentHashtag.IsSame(hashtag) ? "0 0 0 1" : "1 1 1 1" } }, subBackground);
						append = 0.0075f;
						hashtagAnchorMin += (scale * 0.0035f) + hashtagSpacing + append;
					}
				}

				Facepunch.Pool.FreeList(ref originalPosts);
			}
			private void DrawFeed(RusterFeed feed, Page browserFeed, float offset = 0f, bool allowProfilePreviews = true, bool canAddNewPost = false)
			{
				DrawFeed(Container, Background, feed, browserFeed, offset, allowProfilePreviews, canAddNewPost);
			}
			private void DrawFeedButtons(CuiElementContainer container, string parent, Page browserFeed, RusterFeed feed, bool canAddNewPost = false, bool canReply = true)
			{
				if (browserFeed.TotalPages != -1)
				{
					var background = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.01 0.006", AnchorMax = $"0.45 0.03" }, CursorEnabled = true }, parent);
					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {browserFeed.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {browserFeed.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {browserFeed.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {browserFeed.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiLabel { Text = { Text = $"{browserFeed.CurrentPage + 1:n0} / {browserFeed.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.65 0", AnchorMax = $"1 1" } }, background);

					var inputPanel = container.Add(new CuiPanel { Image = { Color = DeselectedButtonColor, }, RectTransform = { AnchorMin = $"0.5015 0", AnchorMax = $"0.61 1" }, CursorEnabled = true }, background);
					container.Add(new CuiElement
					{
						Parent = inputPanel,
						Components =
						{
							new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{SetPageCmd} {User.Id} {browserFeed.Id} ", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", CharsLimit = 4 },
							new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
						}
					});
				}
			}
			private void DrawPost(CuiElementContainer container, string parent, RusterFeed feed, RusterFeed.RusterPost post, float anchorMin, float anchorMax, ref int lineMultiplier, bool allowProfilePreviews = true, bool showReplies = true, bool showPhoto = true, bool isFullPost = false)
			{
				try
				{
					var user = post.GetUser(ServerViewer);
					var isPreviewingServer = ServerViewer.IsViewing();

					if (post.IsAdvert())
					{
						if (user.IsBot) showReplies = allowProfilePreviews = false;
					}
					else if (post.IsMarketplaceListing())
					{
						showPhoto = feed.GetFeedType() == RusterFeed.FeedTypes.Post && !string.IsNullOrEmpty(post.PhotoUrl);
					}

					var background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5" }, RectTransform = { AnchorMin = $"0.01 {anchorMin - (lineMultiplier * 0.05)}", AnchorMax = $"0.99 {anchorMax}" }, CursorEnabled = true }, parent);
					container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(user.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"0.05 {(lineMultiplier == 1 ? "0.715" : "0.66")}", AnchorMax = "0.125 0.9" } } });
					if (user.HasFrame()) container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(user.GetFrame()), new CuiRectTransformComponent { AnchorMin = $"0.05 {(lineMultiplier == 1 ? "0.715" : "0.66")}", AnchorMax = "0.125 0.9", OffsetMin = "-5 -5", OffsetMax = "5 5" } } });

					if (user.IsVerified())
					{
						container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(VerifiedTickUrl), new CuiRectTransformComponent { AnchorMin = $"0.15 0.8", AnchorMax = "0.175 0.875" } } });
					}

					if (post.IsPoll() && post.HasPollEnded())
					{
						var total = post.Poll.GetTotalVotes();
						var pollGraph = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(total > 0 ? "0.95" : "0")}", }, RectTransform = { AnchorMin = $"{(isFullPost ? "0" : "0.05")} 0", AnchorMax = $"{(isFullPost ? "1" : "0.95")} 0.03" }, CursorEnabled = true }, background);
						var push = 0f;

						if (total > 0)
						{
							for (int i = 0; i < post.Poll.Choices.Count; i++)
							{
								var choice = post.Poll.Choices[i];
								var colorR = RandomEx.GetRandomFloat(0.3f, 1f, i * 100);
								var colorG = RandomEx.GetRandomFloat(0.3f, 1f, i * 105 + 1);
								var colorB = RandomEx.GetRandomFloat(0.3f, 1f, i * 110 + 2);
								var color = $"{colorR:0.00} {colorG:0.00} {colorB:0.00} 0.95";
								var value = ((float)choice.Votes.Count).Scale(0f, total, 0f, 1f);

								container.Add(new CuiPanel { Image = { Color = color }, RectTransform = { AnchorMin = $"{push} 0", AnchorMax = $"{push + value} 1" }, CursorEnabled = true }, pollGraph);
								push += value;
							}
						}
					}

					var daysLeft = post.AdvertTimeLeft() / 24;
					var hoursLeft = post.AdvertTimeLeft();

					var listingInfo = "";
					if (post.IsMarketplaceListing())
					{
						var license = (RusterLicensedItem)null;
						var isLicensed = Instance.Config.License.IsLicensedItem(post.PhotoUrl, out license);
						var itemName = $"<b>{(string.IsNullOrEmpty(post.MarketplaceListing?.CustomName) ? GetPhrase(post.MarketplaceListing?.GetSoldItemDefinition()) : post.MarketplaceListing?.CustomName)}</b>";
						var itemAmount = $"{(post.MarketplaceListing.WholeStack ? $"{post.MarketplaceListing?.Amount:n0}" : $"{post.MarketplaceListing?.AmountLeft:n0}/{post.MarketplaceListing?.Amount:n0}")}";
						var itemPrice = post.MarketplaceListing?.Price == 0 ? $"<b>{GetPhrase("free")}</b>" : $"{Instance.Config.Currency.GetValueName(this, post.MarketplaceListing.Price, true)}";
						listingInfo = post.MarketplaceListing != null ? $"<size=9>{GetPhrase("listing_item", itemAmount, itemName, itemPrice)} — {(!post.MarketplaceListing.IsPurchased ? $"<color=#{(isLicensed ? "deda09" : "89de09")}>{GetPhrase("instock")}</color>" : $"<color=#de0909>{GetPhrase("notinstock")}</color>")} </size>" : "";
					}

					container.Add(new CuiLabel { Text = { Text = $"<b>{user.GetDisplayName(!isPreviewingServer, observer: User)}</b>{(isPreviewingServer ? "" : $" {user.GetOnlineIcon()}")} {listingInfo}".Trim(), FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"{(user.IsVerified() ? 0.19 : 0.15)} 0", AnchorMax = $"1 0.9" } }, background);
					container.Add(new CuiLabel { Text = { Text = $"{user.GetUsername(observer: User)} {(post.IsCassette() ? $"● <i>{post.CassetteTitle}</i>" : $"")}{(post.IsAdvert() ? $"<color={(post.IsMarketplaceListing() && post.MarketplaceListing.IsPurchased ? "#de0909" : "#89de09")}>● {GetPhrase("advert")}{(user.IsBot ? "" : $" ● {(post.AdvertTimeLeft() > 24 ? $"{((int)daysLeft).Plural(GetPhrase("day_left", daysLeft.ToString("0.0")), GetPhrase("days_left", daysLeft.ToString("0.0")))}" : $"{((int)hoursLeft).Plural(GetPhrase("hour_left", hoursLeft.ToString("0.0")), GetPhrase("hours_left", hoursLeft.ToString("0.0")))}")}")}</color>" : "")}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.15 0", AnchorMax = $"1 {(lineMultiplier == 1 ? "0.775" : "0.76")}" } }, background);
					if (!(user.IsBot && post.IsAdvert())) container.Add(new CuiLabel { Text = { Text = $"{(feed.ShowDate ? User.GetTimeSpan(post.Ticks, User.Id) : "")}{(feed.ShowLocation && post.Location.IsValid() ? $" ● {GetPhrase("near_place", $"{post.Location.Name} ({GetGrid(post.Location.Position.ToVector3())})")}" : "")}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperRight }, RectTransform = { AnchorMin = $"0.05 0", AnchorMax = $"0.95 0.95" } }, background);

					container.Add(new CuiLabel { Text = { Text = $"{post.GetContent()}", FontSize = 17, Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.05 0.275", AnchorMax = $"{(post.IsEmbedded() ? 0.8 : 0.95)} {(lineMultiplier == 1 ? "0.7" : "0.7")}" } }, background);
					if (post.IsEmbedded()) container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(post.PhotoUrl), new CuiRectTransformComponent { AnchorMin = $"0.825 0.4", AnchorMax = $"0.955 {0.8 - (1f - post.EmbedPhotoRatio)}" } } });

					if (feed.AllowPurchases && post.IsMarketplaceListing() && Instance.Config.Tax.IsThereTax())
						container.Add(new CuiLabel { Text = { Color = "1 1 1 0.5", Text = GetPhrase("posttax", Instance.Config.Tax.GetPercentage()), FontSize = 8, Font = DefaultFont, Align = TextAnchor.LowerLeft }, RectTransform = { AnchorMin = $"0.05 0.005", AnchorMax = $"0.95 1" } }, background);

					if (!ServerViewer.IsViewing() && !isFullPost && !post.IsAdvert()) container.Add(new CuiButton { Button = { Command = $"{FullPostCmd} {User.Id} {feed.Id} {post.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Color = "0 0 0 0" } }, background);

					if (user.HasBlockedCommunication(User.Id)) container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : 0.2)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0.025 0", AnchorMax = $"0.975 0.975" }, CursorEnabled = true }, background);

					if (allowProfilePreviews)
					{
						container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {post.UserId}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"{(user.IsVerified() ? 0.19 : 0.15)} 0.75", AnchorMax = $"1 0.9" }, Text = { Text = string.Empty, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
						container.Add(new CuiButton { Button = { Command = $"{ProfileCmd} {User.Id} {post.UserId}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.05 {(lineMultiplier == 1 ? "0.715" : "0.66")}", AnchorMax = "0.125 0.9" }, Text = { Text = $"", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
					}

					if (feed.AllowPinning && (post.IsPinned || post.CanPin(User, feed)))
					{
						var pinButton = container.Add(new CuiButton { Button = { Command = ServerViewer.IsViewing() ? "" : $"{PinPostCmd} {User.Id} {post.Id} {feed.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.025 {(lineMultiplier == 1 ? "0.715" : "0.825")}", AnchorMax = "0.065 0.95" }, Text = { Text = string.Empty, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);
						container.Add(new CuiElement { Parent = pinButton, Components = { Death.GetRawImage(PinButtonIconUrl, color: post.IsPinned ? "1 1 1 1" : post.CanPin(User, feed) && !ServerViewer.IsViewing() ? "1 1 1 0.2" : "0 0 0 0"), new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });
					}

					if (post.CassetteId != 0)
					{
						container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(!AudioPlayer.IsPlaying(post) ? PlayButtonIconUrl : StopButtonIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.11 {(lineMultiplier == 1 ? "0.685" : "0.62")}", AnchorMax = "0.14 0.71", OffsetMin = "-10 0", OffsetMax = "-10 0" } } });
						if (feed.AllowPlay) container.Add(new CuiButton { Button = { Command = $"{(!AudioPlayer.IsPlaying(post) ? PlayPostCmd : StopPostCmd)} {User.Id} {post.Id} true", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.11 {(lineMultiplier == 1 ? "0.685" : "0.62")}", AnchorMax = "0.14 0.71", OffsetMin = "-12 0", OffsetMax = "-12 0" }, Text = { Text = $"", Color = "0 0 0 0" } }, background);
					}

					DrawPostButtons(container, background, feed, post, user, lineMultiplier, showReplies, showPhoto, isFullPost);

					if (!string.IsNullOrEmpty(post.PhotoUrl))
					{
						container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(post.Gif != null && post.Gif.IsFlipbook ? FlipbookIconUrl : ScreenshotIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.11 {(lineMultiplier == 1 ? "0.665" : "0.62")}", AnchorMax = $"0.14 {(lineMultiplier == 1 ? "0.74" : "0.71")}" } } });
						if (PostId != post.Id && !isFullPost) container.Add(new CuiButton { Button = { Command = $"{FullPostCmd} {User.Id} {feed.Id} {post.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.11 {(lineMultiplier == 1 ? "0.685" : "0.62")}", AnchorMax = "0.14 0.71" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, background);
					}
				}
				catch (Exception ex)
				{
					Instance.Puts(ex.ToString());
				}
			}
			private void DrawPostButtons(CuiElementContainer container, string parent, RusterFeed feed, RusterFeed.RusterPost post, RusterUser user, int lineMultiplier, bool showReplies = true, bool showPhoto = true, bool isFullPost = false)
			{
				var background = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.05 {(lineMultiplier == 1 ? "0.1" : "0.1")}", AnchorMax = $"0.95 {(lineMultiplier == 1 ? "0.275" : "0.3")}" }, CursorEnabled = true }, parent);

				if (!user.HasBlockedCommunication(User.Id) && post.CanRate)
				{
					var likesButton = container.Add(new CuiButton { Button = { Command = feed.AllowRatings ? $"{LikeCmd} {User.Id} {post.Id} {feed.Id}" : "", Color = !feed.ShowRatings || !post.HasLiked(User) ? DeselectedButtonColor : NormalButtonColor }, RectTransform = { AnchorMin = "0.84 0", AnchorMax = $"0.915 1" }, Text = { Text = string.Empty } }, background);
					container.Add(new CuiLabel { Text = { Text = $"▲", FontSize = 13, Color = !feed.ShowRatings || !post.HasLiked(User) ? "1 1 1 0.5" : "0 0 0 1", Font = DefaultFont, Align = TextAnchor.UpperCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 0.9" } }, likesButton);
					container.Add(new CuiLabel { Text = { Text = $"{CountFormat(post.Likes.Count)}", FontSize = 8, Color = !feed.ShowRatings || !post.HasLiked(User) ? "1 1 1 0.5" : "0 0 0 1", Font = DefaultFont, Align = TextAnchor.LowerCenter }, RectTransform = { AnchorMin = $"0 0.03", AnchorMax = $"1 1" } }, likesButton);

					var dislikesButton = container.Add(new CuiButton { Button = { Command = feed.AllowRatings ? $"{DislikeCmd} {User.Id} {post.Id} {feed.Id}" : "", Color = !feed.ShowRatings || !post.HasDisliked(User) ? DeselectedButtonColor : CloseButtonColor }, RectTransform = { AnchorMin = "0.92 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty } }, background);
					container.Add(new CuiLabel { Text = { Text = $"▼", FontSize = 13, Color = !feed.ShowRatings || !post.HasDisliked(User) ? "1 1 1 0.5" : "0 0 0 1", Font = DefaultFont, Align = TextAnchor.UpperCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 0.9" } }, dislikesButton);
					container.Add(new CuiLabel { Text = { Text = $"{CountFormat(post.Dislikes.Count)}", FontSize = 8, Color = !feed.ShowRatings || !post.HasDisliked(User) ? "1 1 1 0.5" : "0 0 0 1", Font = DefaultFont, Align = TextAnchor.LowerCenter }, RectTransform = { AnchorMin = $"0 0.03", AnchorMax = $"1 1" } }, dislikesButton);

					container.Add(new CuiButton { Button = { Command = $"{PostLikesDislikesCmd} {User.Id} {post.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.978 0", AnchorMax = $"1 1", OffsetMin = "9 0", OffsetMax = "9 1" }, Text = { Text = string.Empty } }, background);
				}

				if (feed.AllowDeleting && feed.CanDelete(User, post))
				{
					var deleteButton = container.Add(new CuiButton { Button = { Command = $"{DeleteCmd} {User.Id} {post.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.76 0", AnchorMax = $"0.837 1" }, Text = { Text = string.Empty, FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiElement { Parent = deleteButton, Components = { Death.GetRawImage(BinUrl, color: "1 1 1 1"), new CuiRectTransformComponent { AnchorMin = $"0.23 0.25", AnchorMax = "0.75 0.75" } } });
				}
				if (!user.HasBlockedCommunication(User.Id) && showReplies)
				{
					var replyFeed = Instance.Data.GetFeed(post);
					if (!post.IsPoll()) container.Add(new CuiButton { Button = { Command = $"{FullPostCmd} {User.Id} {feed.Id} {post.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.17 1" }, Text = { Text = $"{replyFeed.Posts.Count.Plural(GetPhrase("reply_count", replyFeed.Posts.Count), GetPhrase("replies_count", replyFeed.Posts.Count.ToString("n0")))}", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					else
					{
						var votes = post.Poll.GetTotalVotes();
						container.Add(new CuiButton { Button = { Command = $"{FullPostCmd} {User.Id} {feed.Id} {post.Id}", Color = post.HasPollEnded() ? "0.1 0.73 0.92 0.9" : "0.92 0.73 0.1 0.9" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.17 1" }, Text = { Text = $"<b>POLL</b><size=7> {votes:n0} {votes.Plural("vote", "votes")}</size>", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = post.HasPollEnded() ? "1 1 1 1" : "0 0 0 1" } }, background);
					}
				}

				if (!user.HasBlockedCommunication(User.Id) && ((post.IsMarketplaceListing() && !showPhoto) || post.IsAdvert()))
				{
					if (post.UserId != User.Id || User.IsAdmin())
					{
						if (!post.MarketplaceListing.IsPurchased)
						{
							if (feed.AllowPurchases)
							{
								var button = container.Add(new CuiButton { Button = { Command = $"{BuyPostCmd} {User.Id} {post.Id}", Color = post.CanBuy(User) ? NormalButtonColor : "0 0 0 0" }, RectTransform = { AnchorMin = showReplies ? "0.18 0" : "0 0", AnchorMax = showReplies ? $"0.35 1" : $"0.17 1" }, Text = { Text = $"{GetPhrase("buy")}   ", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleRight, Color = post.CanBuy(User) ? "0 0 0 1" : "1 1 1 1" } }, background);
								if (post.MarketplaceListing.Skin != 0)
								{
									var skinButton = container.Add(new CuiButton { Button = { Command = $"{SkinPreviewCmd} {User.Id} {post.MarketplaceListing.Skin}", Color = "0.5 0.65 0.05 0.75" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = "0.4 1", OffsetMax = "0 -0.2" }, Text = { Color = "0 0 0 0" } }, button);
									container.Add(new CuiElement { Parent = skinButton, Components = { Death.GetRawSkinImage(post.MarketplaceListing.Skin, string.IsNullOrEmpty(post.MarketplaceListing.SkinIconUrl) ? $"{CodeflingRustAPI}/{post.MarketplaceListing.Shortname}.png" : post.MarketplaceListing.SkinIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.1 0.175", AnchorMax = "0.9 0.825" } } });
								}
								else container.Add(new CuiElement { Parent = button, Components = { Death.GetRawSkinImage(post.MarketplaceListing.Skin, string.IsNullOrEmpty(post.MarketplaceListing.SkinIconUrl) ? $"{CodeflingRustAPI}/{post.MarketplaceListing.Shortname}.png" : post.MarketplaceListing.SkinIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.1 0.2", AnchorMax = "0.4 0.8" } } });

								if (!post.MarketplaceListing.WholeStack && !(!user.IsBot && post.MarketplaceListing.CanRestock() && !showReplies))
								{
									var input = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.8", }, RectTransform = { AnchorMin = showReplies ? "0.18 0" : "0 0", AnchorMax = showReplies ? $"0.35 1" : $"0.17 1", OffsetMin = "64 0", OffsetMax = "64 0" }, CursorEnabled = true }, background);
									container.Add(new CuiElement
									{
										Parent = input,
										Components =
										{
											new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{CurrentStackChangeCmd} {User.Id} {post.Id} ", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 7 },
											new CuiRectTransformComponent { AnchorMin = "0.03 0.05", AnchorMax = "0.96 0.5" }
										}
									});
									container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("selected")}: <b>{CurrentStackAmount.Clamp(1, post.MarketplaceListing.AmountLeft):n0}</b>", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = "0.03 0.5", AnchorMax = $"1 0.9" } }, input);
								}
							}
						}
						else container.Add(new CuiLabel { Text = { Text = GetPhrase("notavailable"), FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = showReplies ? "0.18 0" : "0 0", AnchorMax = showReplies ? $"0.35 1" : $"0.17 1" } }, background);
					}

					if (feed.AllowRestocking && (post.MarketplaceListing.IsPurchased || post.MarketplaceListing.CanRestock()) && post.UserId == User.Id && !showReplies) container.Add(new CuiButton { Button = { Command = $"{RestockPostCmd} {User.Id} {post.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.2 0", AnchorMax = $"0.35 1" }, Text = { Text = GetPhrase("restock_btn"), FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
				}
			}
			private void DrawNewPost(RusterFeed feed)
			{
				var fieldMin = "0.4 0.55";
				var fieldMax = "0.6 0.6";

				Container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("willpostto", feed.GetFeedTitle(User))} {(Instance.Config.Tax.IsThereTax() && MarketplaceItem != null ? GetPhrase("newposttax", Instance.Config.Tax.GetPercentage()) : "")}".Trim(), FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.4 0.4", AnchorMax = $"0.6 0.675" } }, Background);

				var background2 = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.4 0.53", AnchorMax = $"0.6 0.66" }, CursorEnabled = true }, Background);
				Container.Add(new CuiElement { Parent = background2, Components = { Death.GetRawImage(User.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"0.03 0.625", AnchorMax = "0.13 0.9" } } });
				if (User.HasFrame()) Container.Add(new CuiElement { Parent = background2, Components = { Death.GetRawImage(User.GetFrame()), new CuiRectTransformComponent { AnchorMin = $"0.03 0.625", AnchorMax = "0.13 0.9", OffsetMin = "-5 -5", OffsetMax = "5 5" } } });

				Container.Add(new CuiLabel { Text = { Text = $"<b>{User.GetDisplayName(observer: User)}</b>", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.16 0", AnchorMax = $"1 0.9" } }, background2);
				Container.Add(new CuiLabel { Text = { Text = $"{User.GetUsername(observer: User)}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.16 0", AnchorMax = $"1 0.725" } }, background2);

				var background = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.95", }, RectTransform = { AnchorMin = fieldMin, AnchorMax = fieldMax }, CursorEnabled = true }, Background);
				Container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("content")}: {Content}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.03 0.4", AnchorMax = $"0.96 0.9" } }, background);
				Container.Add(new CuiElement
				{
					Parent = background,
					Components =
					{
						new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{NewPostContentCmd} {User.Id}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = PostLength },
						new CuiRectTransformComponent { AnchorMin = "0.03 0.05", AnchorMax = "0.96 0.7" }
					}
				});

				DrawNewPostButtons(feed);

				if (Instance.HasPermission(Player, PollPerm, true)) Container.Add(new CuiButton { Button = { Command = $"{NewPostPollCmd} {User.Id}", Color = Poll != null ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"0.467 0.46", AnchorMax = $"0.4975 0.49" }, Text = { Text = GetPhrase("poll"), Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, Background);
				if (Instance.Config.Features.EnableLocation) Container.Add(new CuiButton { Button = { Command = $"{NewPostLocationCmd} {User.Id}", Color = Location ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"0.5 0.46", AnchorMax = $"0.5475 0.49" }, Text = { Text = GetPhrase("location"), Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, Background);
				if (Instance.Config.Features.EnableAddPhoto) Container.Add(new CuiButton { Button = { Command = $"{NewPostAddPictureCmd} {User.Id}", Color = string.IsNullOrEmpty(UploadedPhotograph) ? ArrowButtonColor : NormalButtonColor }, RectTransform = { AnchorMin = $"0.55 0.46", AnchorMax = $"0.6 0.49" }, Text = { Text = string.IsNullOrEmpty(UploadedPhotograph) ? GetPhrase("addphoto") : GetPhrase("changephoto"), Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, Background);
				if (MarketplaceItem == null)
				{
					if (Instance.Config.Features.EnableAddCassette) Container.Add(new CuiButton { Button = { Command = $"{NewPostAddCassetteCmd} {User.Id}", Color = UploadedCassetteId == 0 ? ArrowButtonColor : NormalButtonColor }, RectTransform = { AnchorMin = $"0.5 0.425", AnchorMax = $"0.58 0.455" }, Text = { Text = UploadedCassetteId == 0 ? GetPhrase("addcassette") : GetPhrase("changecassette"), Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, Background);

					if (Instance.Config.Features.EnableRecordMemo)
					{
						var micButton = Container.Add(new CuiButton { Button = { Command = $"{NewPostRecordVoiceCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.5825 0.425", AnchorMax = $"0.6 0.455" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, Background);
						Container.Add(new CuiElement { Parent = micButton, Components = { new CuiRawImageComponent { Url = VoiceButtonIconUrl }, new CuiRectTransformComponent { AnchorMin = $"0.2 0.2", AnchorMax = "0.8 0.8" } } });
					}

					if (Instance.Config.Features.EnableAddGifPhoto && User.IsVerified())
					{
						var uploadSongButton = Container.Add(new CuiButton { Button = { Command = $"{NewPostUploadAudioCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.5 0.39", AnchorMax = $"0.6 0.42" }, Text = { Text = $"          {GetPhrase("uploadsong")}", Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" } }, Background);
						Container.Add(new CuiElement { Parent = uploadSongButton, Components = { Death.GetRawImage(VerifiedTickUrl), new CuiRectTransformComponent { AnchorMin = $"0.03 0.2", AnchorMax = "0.12 0.8" } } });

						var addGifButton = Container.Add(new CuiButton { Button = { Command = $"{NewPostAddGIFCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.5 0.39", AnchorMax = $"0.6 0.42", OffsetMin = "0 -23", OffsetMax = "1 -23" }, Text = { Text = $"          Add GIF", Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" } }, Background);
						Container.Add(new CuiElement { Parent = addGifButton, Components = { Death.GetRawImage(VerifiedTickUrl), new CuiRectTransformComponent { AnchorMin = $"0.03 0.2", AnchorMax = "0.12 0.8" } } });
					}
				}

				if (feed.GetFeedType() == RusterFeed.FeedTypes.Shop)
				{
					var validSellingItem = MarketplaceItem != null && !string.IsNullOrEmpty(MarketplaceItem.Shortname);
					Container.Add(new CuiButton { Button = { Command = $"{NewPostSoldRemoveCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = $"0.485 0.425", AnchorMax = $"0.4985 0.455" }, Text = { Text = $"X", Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, Background);
					var selectedItemButton = Container.Add(new CuiButton { Button = { Command = $"{NewPostSoldItemCmd} {User.Id}", Color = validSellingItem ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"0.5 0.425", AnchorMax = $"0.6 0.455" }, Text = { Text = !validSellingItem ? $"  {GetPhrase("selectitem")}" : $"           {MarketplaceItem.Amount:n0} x <size=8><b>{(string.IsNullOrEmpty(MarketplaceItem.CustomName) ? GetPhrase(MarketplaceItem.GetSoldItemDefinition()) : MarketplaceItem.CustomName)}</b></size>", Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleLeft, Color = validSellingItem ? "0 0 0 1" : "1 1 1 0.35" } }, Background);
					if (validSellingItem) Container.Add(new CuiElement { Parent = selectedItemButton, Components = { Death.GetRawSkinImage(MarketplaceItem.Skin, string.IsNullOrEmpty(MarketplaceItem.SkinIconUrl) ? $"{CodeflingRustAPI}/{MarketplaceItem.Shortname}.png" : MarketplaceItem.SkinIconUrl), new CuiRectTransformComponent { AnchorMin = $"0.03 0.1", AnchorMax = "0.16 0.9" } } });

					if (MarketplaceItem != null) Container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("price")}: {(MarketplaceItem.Price == 0 ? $"<b>{GetPhrase("free")}</b>" : $"{Instance.Config.Currency.GetValueName(this, MarketplaceItem.Price, true)}")}", FontSize = 10, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.5 0.3", AnchorMax = $"0.6 0.415" } }, Background);

					var priceBackground = Container.Add(new CuiPanel { Image = { Color = ArrowButtonColor, }, RectTransform = { AnchorMin = $"0.57 0.395", AnchorMax = $"0.6 0.42" }, CursorEnabled = true }, Background);
					Container.Add(new CuiElement
					{
						Parent = priceBackground,
						Components =
						{
							new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{NewPostSoldPriceChangeCmd} {User.Id}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 90 },
							new CuiRectTransformComponent { AnchorMin = $"0.1 0", AnchorMax = $"1 1" }
						}
					});

					Container.Add(new CuiButton { Button = { Command = $"{NewPostSoldWholeStackCmd} {User.Id}", Color = WholeStack ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"0.5 0.36", AnchorMax = $"0.6 0.39" }, Text = { Text = WholeStack ? GetPhrase("wholestack") : GetPhrase("eachitem"), Font = DefaultFont, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, Background);
				}

				LockPlayer();
			}
			private void DrawNewPostButtons(RusterFeed feed)
			{
				var background = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.4 0.5", AnchorMax = $"0.6 0.54" }, CursorEnabled = true }, Background);
				Container.Add(new CuiButton { Button = { Command = $"{NewPostPublishCmd} {User.Id} {feed.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.49 1" }, Text = { Text = $"<b>{GetPhrase("publish")}</b>", Font = DefaultFont, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
				Container.Add(new CuiButton { Button = { Command = $"{NewPostCloseCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.51 0", AnchorMax = $"1 1" }, Text = { Text = GetPhrase("cancel"), FontSize = 13, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, background);
			}
			private void DrawPollEditor(RusterFeed.RusterPost.RusterPoll poll)
			{
				var background = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.35 0.35", AnchorMax = "0.65 0.65" }, CursorEnabled = true }, Background);

				Container.Add(new CuiButton { Button = { Command = $"{NewPostPollAddChoiceCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.015 0.9", AnchorMax = $"0.145 0.97" }, Text = { Text = GetPhrase("poll_addchoice"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
				Container.Add(new CuiButton { Button = { Command = $"{NewPostPollCloseCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.150 0.9", AnchorMax = $"0.24 0.97" }, Text = { Text = GetPhrase("close"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
				Container.Add(new CuiButton { Button = { Command = $"{NewPostPollDurationCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.24 0.9", AnchorMax = $"0.52 0.97" }, Text = { Text = $"{GetPhrase("poll_duration")} <b>{Poll.DurationHours:0.0} {GetPhrase("time_hours")}</b>", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, background);
				Container.Add(new CuiButton { Button = { Command = $"{NewPostPollClearCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.9 0.9", AnchorMax = $"0.985 0.97" }, Text = { Text = GetPhrase("clear"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				var anchorMin = 0.78f;
				var anchorMax = 0.88f;
				var spacing = 0.13f;

				for (int i = 0; i < poll.Choices.Count; i++)
				{
					var choice = poll.Choices[i];
					var panel = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = $"0.015 {anchorMin}", AnchorMax = $"0.985 {anchorMax}" }, CursorEnabled = true }, background);
					Container.Add(new CuiLabel { Text = { Text = $"{Letters[i]}. <b>{choice.Text}</b>", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleLeft, Color = "1 1 1 0.9" }, RectTransform = { AnchorMin = "0.015 0", AnchorMax = $"0.985 1" } }, panel);
					Container.Add(new CuiButton { Button = { Command = $"{NewPostPollAddChoiceMoveUpCmd} {User.Id} {i}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.72 0.1", AnchorMax = $"0.78 0.9" }, Text = { Text = "▲", FontSize = 8, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, panel);
					Container.Add(new CuiButton { Button = { Command = $"{NewPostPollAddChoiceMoveDownCmd} {User.Id} {i}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.79 0.1", AnchorMax = $"0.85 0.9" }, Text = { Text = "▼", FontSize = 8, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, panel);
					Container.Add(new CuiButton { Button = { Command = $"{NewPostPollAddChoiceDeleteCmd} {User.Id} {i}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.87 0.1", AnchorMax = $"0.995 0.9" }, Text = { Text = GetPhrase("delete").ToUpper(), FontSize = 8, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, panel);

					anchorMin -= spacing;
					anchorMax -= spacing;
				}
			}
			public void DrawFullPost(RusterFeed feed, RusterFeed.RusterPost post, bool isBackground = false)
			{
				CloseFullPost(feed, post);

				if (post == null) { OverlayPanelType = OverlayPanelTypes.None; Draw(); return; }

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawFullPost(feed, post, isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, "Overlay", $"{(!isBackground ? FullPostCUI : FullPostGhostCUI)}_{post.Id}_{feed.Id}");
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.02", AnchorMax = "0.99 0.975" }, CursorEnabled = true }, ratioBackground);
				container.Add(new CuiButton { Button = { Command = $"{CloseFullPostCmd} {User.Id} {feed.Id} {post.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }, Text = { Color = "0 0 0 0" } }, background);

				var background2 = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.75", }, RectTransform = { AnchorMin = $"0.175 0.55", AnchorMax = $"0.5 0.75" }, CursorEnabled = true }, background);
				var lineMultiplier = 0;
				DrawPost(container, background2, feed, post, 0f, 1f, ref lineMultiplier, allowProfilePreviews: false, showReplies: false, showPhoto: false, isFullPost: true);

				container.Add(new CuiButton { Button = { Command = $"{CloseFullPostCmd} {User.Id} {feed.Id} {post.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.95 0.9", AnchorMax = $"0.98 0.99" }, Text = { Text = $"X", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background2);
				if (!ServerViewer.IsViewing())
				{
					if (post.CanEdit(User)) container.Add(new CuiButton { Button = { Command = $"{EditPostCmd} {User.Id} {post.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.92 0.79", AnchorMax = $"0.98 0.88" }, Text = { Text = $"EDIT", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, background2);
					else if (post.IsEdited()) container.Add(new CuiLabel { Text = { Text = $"EDITED", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.2" }, RectTransform = { AnchorMin = "0.92 0.79", AnchorMax = $"0.98 0.88" } }, background2);
				}

				if (!ServerViewer.IsViewing() && post.UserId != User.Id) container.Add(new CuiButton { Button = { Command = $"{ReportPostCmd} {User.Id} {feed.Id} {post.Id}", Color = "0.6 0.1 0.1 1" }, RectTransform = { AnchorMin = "0.91 0.79", AnchorMax = $"0.99 0.88", OffsetMin = "-2 0", OffsetMax = "-2 0" }, Text = { Text = $"REPORT", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background2);
				if (post.IsMarketplaceListing()) container.Add(new CuiButton { Button = { Command = User.IsAdmin() || post.UserId == User.Id ? $"{OpenCouponEditorCmd} {User.Id} {feed.Id} {post.Id}" : "", Color = post.MarketplaceListing.Coupon == null ? ArrowButtonColor : "0.2 0.6 0.2 1" }, RectTransform = { AnchorMin = "0.87 0.79", AnchorMax = $"0.98 0.88", OffsetMin = "2 -15", OffsetMax = "2 -15" }, Text = { Text = $"COUPON", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background2);

				if (feed.GetFeedType() != RusterFeed.FeedTypes.Post && feed.ShowReplies)
				{
					var dedicatedFeed = Instance.Data.GetFeed(post);
					if (dedicatedFeed.FeedType != RusterFeed.FeedTypes.Shop) dedicatedFeed.Title = GetPhrase("replies");
					dedicatedFeed.ShowHashtags = false;
					DrawFeed(container, background, dedicatedFeed, GetPage(70), offset: 0.2975f, allowProfilePreviews: false, canReply: false, canAddNewPost: true);
				}

				if (post.IsPoll())
				{
					var canvas = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.175 0.24", AnchorMax = $"0.5 0.55" }, CursorEnabled = true }, background);
					var anchorMin = 0.8f;
					var anchorMax = 1f;
					var spacing = 0.15f;
					var voteTotal = post.Poll.GetTotalVotes();
					var winningChoice = post.Poll.GetWinningChoice();

					if (post.HasPollEnded())
					{
						var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.05 {anchorMin}", AnchorMax = $"0.985 {anchorMax}" }, CursorEnabled = true }, canvas);
						container.Add(new CuiLabel { Text = { Text = $"THE POLL HAS ENDED<size=8>\nWinner: <b>{winningChoice.Text}</b></size>", FontSize = 12, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.9" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" } }, panel);

						anchorMin -= spacing;
						anchorMax -= spacing;
					}

					for (int i = 0; i < post.Poll.Choices.Count; i++)
					{
						var choice = post.Poll.Choices[i];
						var percentage = choice.Votes.Count.Percentage(voteTotal);
						var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.05 {anchorMin}", AnchorMax = $"0.985 {anchorMax}" }, CursorEnabled = true }, canvas);
						if (percentage > 0) container.Add(new CuiPanel { Image = { Color = winningChoice == choice && post.HasPollEnded() ? "0.1 0.73 0.92 0.75" : "0.92 0.73 0.1 0.5", }, RectTransform = { AnchorMin = $"0 0.2", AnchorMax = $"{percentage.Scale(0f, 100f, 0f, 1f)} 0.8" }, CursorEnabled = true }, panel);
						container.Add(new CuiLabel { Text = { Text = $"<b>{choice.Text}</b>", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleLeft, Color = "1 1 1 0.9" }, RectTransform = { AnchorMin = "0.025 0", AnchorMax = $"1 1" } }, panel);

						if (!post.HasUserVoted(User) && !post.HasPollEnded() && !ServerViewer.IsViewing())
						{
							container.Add(new CuiButton { Button = { Command = $"{VoteCmd} {User.Id} {post.Id} {feed.Id} {i}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.8 0.3", AnchorMax = $"0.9 0.7" }, Text = { Text = "<b>VOTE</b>", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, panel);
						}
						else
						{
							container.Add(new CuiLabel { Text = { Text = $"{(choice.HasVoted(User.Id) && !ServerViewer.IsViewing() ? "Your vote —" : "")} {(!float.IsNaN(percentage) ? $"{percentage:0.0}%" : "")}", FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleRight, Color = "1 1 1 0.5" }, RectTransform = { AnchorMin = "0 0.3", AnchorMax = $"0.985 0.7" } }, panel);
						}

						anchorMin -= spacing;
						anchorMax -= spacing;
					}
				}

				if (!string.IsNullOrEmpty(post.PhotoUrl))
				{
					container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(post.PhotoUrl), new CuiRectTransformComponent { AnchorMin = $"0.175 0.24", AnchorMax = $"0.5 0.55" } } });
					container.Add(new CuiButton { Button = { Command = $"{OpenPictureViewerCmd} {User.Id} {post.PhotoUrl}", Color = "0 0 0 0" }, Text = { Text = string.Empty, Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.175 0.24", AnchorMax = $"0.5 0.55" } }, background);

					if (post.IsGif())
					{
						var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.75", }, RectTransform = { AnchorMin = $"0.175 0.24", AnchorMax = $"0.5 0.55" }, CursorEnabled = true }, background);
						container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : 0.1)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, CursorEnabled = true }, panel);
						container.Add(new CuiElement { Parent = panel, Components = { Death.GetRawImage(PlayButtonIconUrl, color: "1 1 1 0.7"), new CuiRectTransformComponent { AnchorMin = $"0.4 0.275", AnchorMax = $"0.6 0.625" } } });
						container.Add(new CuiButton { Button = { Command = $"{OpenPostGifPanelCmd} {User.Id} {post.Id}", Color = "0 0 0 0" }, Text = { Text = string.Empty, Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.4 0.275", AnchorMax = $"0.6 0.625" } }, panel);

						var gifBadge = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.7", }, RectTransform = { AnchorMin = $"0.47 0.25", AnchorMax = $"0.49 0.27" }, CursorEnabled = true }, background);
						container.Add(new CuiLabel { Text = { Text = $"GIF", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.9" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" } }, gifBadge);
					}
				}
				if (!string.IsNullOrEmpty(post.PhotoTag)) container.Add(new CuiLabel { Text = { Text = $"<i>{post.PhotoTag}</i>", FontSize = 8, Font = DefaultFont, Align = TextAnchor.UpperCenter }, RectTransform = { AnchorMin = $"0.165 0", AnchorMax = $"0.5 0.23" } }, background);

				if (!isBackground || (isBackground && User.Configuration.PerformanceMode)) FullPostHistory.Add(post, feed);

				CuiHelper.AddUi(Player, container);
			}
			public void RedrawLastFullPost()
			{
				var fullPost = FullPostHistory.LastOrDefault();

				DrawFullPost(fullPost.Value, fullPost.Key);
			}
			public void RedrawAllFullPosts()
			{
				foreach (var post in FullPostHistory.Reverse())
				{
					DrawFullPost(post.Value, post.Key);
				}
			}
			public bool AlreadyDrawingFullPost(RusterFeed.RusterPost post)
			{
				return FullPostHistory.ContainsKey(post);
			}
			private void DrawProfile(RusterUser user, bool isBackground = false)
			{
				CloseProfile();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawProfile(user, isBackground: true);

				var isPreviewingServer = ServerViewer.IsViewing();
				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? ProfileCUI : ProfileGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.02", AnchorMax = "0.99 0.975" }, CursorEnabled = true }, ratioBackground);
				container.Add(new CuiButton { Button = { Command = $"{CloseProfileCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" }, Text = { Color = "0 0 0 0" } }, background);

				var tags = Facepunch.Pool.GetList<string>();
				if (!user.IsBot)
				{
					if (user.IsVerified()) tags.Add($"<color=#17a6d1>{GetPhrase("verifiedacc")}</color>");
					if (!ServerViewer.IsViewing())
					{
						if (user.IsFriends(User.Id)) tags.Add(GetPhrase("tag_friend"));
						if (user.IsDeveloper()) tags.Add("developer");
						if (user.IsAdmin()) tags.Add(GetPhrase("tag_admin"));
						if (user.IsModerator()) tags.Add(GetPhrase("tag_moderator"));

						if (user.IsDead()) tags.Add(GetPhrase("tag_dead"));
						if (user.IsOnline()) tags.Add(GetPhrase("tag_online"));
					}
				}
				else
				{
					tags.Add("<color=#17a6d1>Bot</color>");
				}

				var banner = container.Add(new CuiPanel { Image = { Color = "0.08 0.08 0.08 0.65", }, RectTransform = { AnchorMin = $"0.3 0.53", AnchorMax = $"0.5 0.66", OffsetMin = "0 90", OffsetMax = "0 90" }, CursorEnabled = true }, background);
				container.Add(new CuiElement { Parent = banner, Components = { Death.GetRawImage(user.CustomBannerUrl), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = "1 1" } } });
				container.Add(new CuiElement { Parent = banner, Components = { Death.GetRawImage(BannerNoiseUrl, color: "1 1 1 0.2"), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = "1 1" } } });
				container.Add(new CuiButton { Button = { Command = $"{OpenPictureViewerCmd} {User.Id} {user.CustomBannerUrl}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = "1 1" }, Text = { Color = "0 0 0 0" } }, banner);
				if (!ServerViewer.IsViewing() && (User == user || User.IsAdmin()))
				{
					container.Add(new CuiButton { Button = { Command = $"{OpenBannerListCmd} {User.Id} {user.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.85 0.075", AnchorMax = $"0.975 0.26" }, Text = { Text = "EDIT", Align = TextAnchor.MiddleCenter, FontSize = 8, Color = "1 1 1 0.75" } }, banner);
					container.Add(new CuiButton { Button = { Command = $"{OpenAvatarListCmd} {User.Id} {user.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.025 0.075", AnchorMax = $"0.25 0.26" }, Text = { Text = "EDIT AVATAR", Align = TextAnchor.MiddleCenter, FontSize = 8, Color = "1 1 1 0.75" } }, banner);
					container.Add(new CuiButton { Button = { Command = $"{OpenFrameListCmd} {User.Id} {user.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.2675 0.075", AnchorMax = $"0.415 0.26" }, Text = { Text = "FRAME", Align = TextAnchor.MiddleCenter, FontSize = 8, Color = "1 1 1 0.75" } }, banner);
				}
				var about = container.Add(new CuiPanel { Image = { Color = "0.08 0.08 0.08 0.65", }, RectTransform = { AnchorMin = $"0.3 0.53", AnchorMax = $"0.5 0.66", OffsetMin = "0 -90", OffsetMax = "0 -90" }, CursorEnabled = true }, background);
				container.Add(new CuiLabel { Text = { Text = $"<b><color=grey>ABOUT ME</color></b>\n\n<size=10>{user.AboutMe}</size>", FontSize = 8, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.05 0", AnchorMax = $"1 0.925" } }, about);
				if (!ServerViewer.IsViewing() && (User == user || User.IsAdmin()))
				{
					container.Add(new CuiButton { Button = { Command = $"{EditAboutMeCmd} {User.Id} {user.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"0.85 0.075", AnchorMax = $"0.975 0.26" }, Text = { Text = "EDIT", Align = TextAnchor.MiddleCenter, FontSize = 8, Color = "1 1 1 0.75" } }, about);
				}
				if (!ServerViewer.IsViewing() && User != user) container.Add(new CuiButton { Button = { Command = $"{ReportUserCmd} {User.Id} {user.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = $"0.025 0.075", AnchorMax = $"0.275 0.26" }, Text = { Text = "REPORT ABUSE", Align = TextAnchor.MiddleCenter, FontSize = 8, Color = "1 1 1 1" } }, about);

				var tagArray = tags.ToArray();
				var tagString = tagArray.Length > 0 ? tagArray.ToString(", ", ", ").ToCamelCase() : tagArray.ToString(", ", ", ");
				var background2 = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.75", }, RectTransform = { AnchorMin = $"0.3 0.53", AnchorMax = $"0.5 0.66" }, CursorEnabled = true }, background);
				container.Add(new CuiElement { Parent = background2, Components = { Death.GetRawImage(user.GetAvatar()), new CuiRectTransformComponent { AnchorMin = $"0.05 0.625", AnchorMax = "0.14 0.9" } } });
				if (user.HasFrame()) container.Add(new CuiElement { Parent = background2, Components = { Death.GetRawImage(user.GetFrame()), new CuiRectTransformComponent { AnchorMin = $"0.05 0.625", AnchorMax = "0.14 0.9", OffsetMin = "-5 -5", OffsetMax = "5 5" } } });
				container.Add(new CuiLabel { Text = { Text = $"<b>{user.GetDisplayName(!isPreviewingServer, observer: User)}</b>{(isPreviewingServer ? "" : $" {user.GetOnlineIcon()}")}", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.16 0", AnchorMax = $"1 0.9" } }, background2);
				container.Add(new CuiLabel { Text = { Text = $"{user.GetUsername(observer: User)}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.16 0", AnchorMax = $"1 0.725" } }, background2);
				container.Add(new CuiLabel { Text = { Text = $"{tagString}\n", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.05 0", AnchorMax = $"1 0.575" } }, background2);
				container.Add(new CuiButton { Button = { Command = $"{UserAvatarCmd} {User.Id} {user.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.05 0.625", AnchorMax = "0.14 0.9" }, Text = { Color = "0 0 0 0" } }, background2);

				if (!ServerViewer.IsViewing())
				{
					DrawProfileButtons(container, background, user);
					DrawFeed(container, background, Instance.Data.GetFeed(user), GetPage(3), offset: 0.3f, allowProfilePreviews: false, canAddNewPost: user.IsFriends(User.Id) || user.Id == User.Id);
				}

				tagArray = null;
				Facepunch.Pool.FreeList(ref tags);

				CuiHelper.AddUi(Player, container);
			}
			private void DrawProfileButtons(CuiElementContainer container, string parent, RusterUser user)
			{
				var background = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.3 0.55", AnchorMax = $"0.5 0.5775" }, CursorEnabled = true }, parent);

				if (user.Id != User.Id)
				{
					if (!User.HasBlockedCommunication(user.Id))
					{
						if (Instance.Data.HasFriendRequestSent(user, User))
						{
							container.Add(new CuiButton { Button = { Command = $"{HandleFriendRequestCmd} {User.Id} {user.Id} true", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.05 0", AnchorMax = $"0.35 1" }, Text = { Text = GetPhrase("acceptrequest"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
							container.Add(new CuiButton { Button = { Command = $"{HandleFriendRequestCmd} {User.Id} {user.Id} false", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.37 0", AnchorMax = $"0.65 1" }, Text = { Text = GetPhrase("rejectrequest"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
						}
						else if (Instance.Data.HasFriendRequestSent(User, user))
						{
							container.Add(new CuiLabel { Text = { Text = GetPhrase("requestsent"), FontSize = 9, Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.05 0", AnchorMax = $"0.25 1" } }, background);
							container.Add(new CuiButton { Button = { Command = $"{CancelFriendRequestCmd} {User.Id} {user.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.3 0", AnchorMax = $"0.56 1" }, Text = { Text = GetPhrase("cancelrequest"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
						}
						else if (!user.IsBot)
						{
							if (!user.IsFriends(User.Id)) container.Add(new CuiButton { Button = { Command = $"{AddFriendCmd} {User.Id} {user.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.05 0", AnchorMax = $"0.25 1" }, Text = { Text = GetPhrase("addfriend"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
							else container.Add(new CuiButton { Button = { Command = $"{RemoveFriendCmd} {User.Id} {user.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.05 0", AnchorMax = $"0.3 1" }, Text = { Text = GetPhrase("removefriend"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
						}
					}

					if (Instance.HasPermission(Player, TradePerm, true) && User.IsFriends(user)) container.Add(new CuiButton { Button = { Command = $"{TradeCmd} {User.Id} {user.Id}", Color = "0.3 0.3 1 1" }, RectTransform = { AnchorMin = "0.55 0", AnchorMax = $"0.69 1" }, Text = { Text = "Trade", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, background);
					if ((!user.IsBot && !User.HasBlockedCommunication(user.Id) && user.Id != User.Id && (Instance.Config.DMs.MustBeFriendsToDM ? User.IsFriends(user.Id) : true)) || User.IsAdmin() || User.IsModerator()) container.Add(new CuiButton { Button = { Command = $"{DMCmd} {User.Id} {user.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.7 0", AnchorMax = $"0.79 1" }, Text = { Text = "DM", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{BlockCmd} {User.Id} {user.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.8 0", AnchorMax = $"0.95 1" }, Text = { Text = User.HasBlocked(user.Id) ? GetPhrase("dounblock") : GetPhrase("doblock"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
				}

				if (!IsBusinessCardCooldown(false) && user.AllowBusinessCard) container.Add(new CuiButton { Button = { Command = $"{CreateProfileCardCmd} {User.Id} {user.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = $"0.455 0.625", AnchorMax = $"0.4765 0.6475" }, Text = { Text = GetPhrase("card"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, parent);
				container.Add(new CuiButton { Button = { Command = $"{CloseProfileCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = $"0.4775 0.625", AnchorMax = $"0.49 0.6475" }, Text = { Text = $"X", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, parent);
			}
			public void DrawMessageReaction(bool isBackground = false)
			{
				CloseMessageReaction();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawMessageReaction(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? MessageReactionCUI : MessageReactionGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.4 0.45", AnchorMax = "0.6 0.55" }, CursorEnabled = true }, ratioBackground);

				var spacing = 0.105f;
				var anchorMin = 0.04f;
				var anchorMax = 0.11f;
				var allEmojis = Instance.Emojis;
				var page = GetPage(57);
				page.TotalPages = (int)Math.Ceiling((double)allEmojis.Count / EmojisPerPage - 1);

				var pageEmojis = allEmojis.Skip(EmojisPerPage * page.CurrentPage).Take(EmojisPerPage).ToArray();
				page.Check();

				foreach (var emoji in pageEmojis)
				{
					container.Add(new CuiElement { Parent = background, Components = { new CuiRawImageComponent { Url = emoji.IconUrl }, new CuiRectTransformComponent { AnchorMin = $"{anchorMin} 0.5", AnchorMax = $"{anchorMax} 0.75" } } });
					container.Add(new CuiButton { Button = { Command = $"{UpdateMessageReactionCmd} {User.Id} {emoji.Shortname}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"{anchorMin} 0.5", AnchorMax = $"{anchorMax} 0.75" }, Text = { Text = $"", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);

					anchorMin += spacing;
					anchorMax += spacing;
				}

				if (page.TotalPages > -1)
				{
					var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.27 0.1", AnchorMax = $"0.95 0.35" }, CursorEnabled = true }, background);

					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseMessageReactionCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = $"0.25 0.35" }, Text = { Text = GetPhrase("close"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawConfirmDialog(string title, string subtitle, Action onAccept = null, Action onCancel = null, bool isBackground = false)
			{
				CloseConfirmDialog();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawConfirmDialog(title, subtitle, onAccept, onCancel, isBackground: true);

				OnDialogAccept = onAccept;
				OnDialongCancel = onCancel;

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? ConfirmDialogCUI : ConfirmDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.6" }, CursorEnabled = true }, ratioBackground);

				container.Add(new CuiLabel { Text = { Text = $"<b>{title}</b>", FontSize = 15, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.9" } }, background);
				container.Add(new CuiLabel { Text = { Text = $"{subtitle}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.725" } }, background);

				container.Add(new CuiButton { Button = { Command = $"{AcceptConfirmDialogCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.025 0.1", AnchorMax = $"0.15 0.25" }, Text = { Text = $"<b>{GetPhrase("accept").ToUpper()}</b>", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseConfirmDialogCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.16 0.1", AnchorMax = $"0.28 0.25" }, Text = { Text = $"<b>{GetPhrase("cancel").ToUpper()}</b>", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawScreenNotice(string text, bool isBackground = false, float verticalOffset = 20f)
			{
				CloseScreenNotice();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawScreenNotice(text, isBackground: true, verticalOffset: verticalOffset);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0.425 0.1", AnchorMax = "0.575 0.18", OffsetMin = $"0 {verticalOffset}", OffsetMax = $"0 {verticalOffset}" },
					FadeOut = MainFadeout,
					CursorEnabled = false
				}, "Hud", !isBackground ? ScreenNoticeCUI : ScreenNoticeGhostCUI);

				container.Add(new CuiLabel { Text = { Text = text, FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawFlipbookNotice(RusterFlipbook flipbook)
			{
				var inventoryFull = Player.inventory.containerMain.IsFull() && Player.inventory.containerBelt.IsFull();

				// if ( inventoryFull )
				// {
				//     DrawScreenNotice (
				//         $"<color=orange><b>Flipbook</b></color>\n" +
				//         $"<size=15><color=red>INVENTORY FULL</color></size>\n" +
				//         $" <size=8>{( flipbook.MaximumFrames == 20 ? "Short" : flipbook.MaximumFrames == 40 ? "Medium" : flipbook.MaximumFrames == 180 ? "Long" : "Custom" )}</size>" );
				//     return;
				// }

				DrawScreenNotice(
					$"<color=orange><b>Flipbook</b></color>\n" +
					$"<size=15>{flipbook.Frames}</size><size=12> / {flipbook.MaximumFrames:n0}</size>\n" +
					$" <size=8>{(flipbook.MaximumFrames == 20 ? "Short" : flipbook.MaximumFrames == 40 ? "Medium" : flipbook.MaximumFrames == 180 ? "Long" : "Custom")}</size>");
			}
			public void DrawLanguageDialog(bool isBackground = false)
			{
				CloseLanguageDialog();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawLanguageDialog(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? LanguageDialogCUI : LanguageDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.4 0.45", AnchorMax = "0.6 0.55" }, CursorEnabled = true }, ratioBackground);

				var spacing = 0.105f;
				var anchorMin = 0.04f;
				var anchorMax = 0.11f;
				var allLanguages = Instance.Config.Localisation.Languages.OrderBy(x => x.Name).ToArray();
				var page = GetPage(58);
				page.TotalPages = (int)Math.Ceiling((double)allLanguages.Length / LanguagesPerPage - 1);

				var pageLanguages = allLanguages.Skip(LanguagesPerPage * page.CurrentPage).Take(LanguagesPerPage).ToArray();
				page.Check();

				var odd = false;
				foreach (var language in pageLanguages)
				{
					container.Add(new CuiElement { Parent = background, Components = { new CuiRawImageComponent { Url = language.FlagUrl }, new CuiRectTransformComponent { AnchorMin = $"{anchorMin} 0.5", AnchorMax = $"{anchorMax} 0.75" } } });
					container.Add(new CuiLabel { Text = { Text = $"{language.Name}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = odd ? TextAnchor.LowerLeft : TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"{anchorMin} 0.4", AnchorMax = $"{anchorMax * 1.5f} 0.85" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{LanguageDialogChangeCmd} {User.Id} {language.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"{anchorMin} 0.5", AnchorMax = $"{anchorMax} 0.75" }, Text = { Text = $"", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" } }, background);

					odd = !odd;

					anchorMin += spacing;
					anchorMax += spacing;
				}

				if (page.TotalPages > -1)
				{
					var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.27 0.1", AnchorMax = $"0.95 0.35" }, CursorEnabled = true }, background);

					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
				}

				container.Add(new CuiButton { Button = { Command = $"{LanguageDialogCloseCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = $"0.25 0.35" }, Text = { Text = GetPhrase("close"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawPostLikesAndDislikes(bool isBackground = false)
			{
				ClosePostLikesAndDislikes();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawPostLikesAndDislikes(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? PostLikesAndDislikesDialogCUI : PostLikesAndDislikesDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{ClosePostLikesDislikesCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.225 0.1", AnchorMax = "0.775 0.875" }, CursorEnabled = true }, ratioBackground);

				var post = ServerViewer.IsViewing() ? (ServerViewer.CommunityFeed.Posts.FirstOrDefault(x => x.Id == PostId) ?? ServerViewer.MarketplaceFeed.Posts.FirstOrDefault(x => x.Id == PostId)) : Instance.Data.GetPost(PostId);
				var anchorMin = 0.835f;
				var anchorMax = 0.925f;
				var spacing = 0.07f;

				var likes = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.075", AnchorMax = "0.32 0.98" }, CursorEnabled = true }, background);
				{
					DrawUsers(GetPhrase("likes_title"), post.GetLikes(ServerViewer), 12, GetPhrase("likes_nocontent"), container, likes, 59, anchorMin: anchorMin, anchorMax: anchorMax, spacing: spacing);
				}

				var dislikes = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.34 0.075", AnchorMax = "0.65 0.98" }, CursorEnabled = true }, background);
				{
					DrawUsers(GetPhrase("dislikes_title"), post.GetDislikes(ServerViewer), 12, GetPhrase("dislikes_nocontent"), container, dislikes, 60, anchorMin: anchorMin, anchorMax: anchorMax, spacing: spacing);
				}

				var mentions = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.67 0.075", AnchorMax = "0.985 0.98" }, CursorEnabled = true }, background);
				{
					DrawUsers(GetPhrase("mentions_title"), post.GetMentions(), 12, GetPhrase("mentions_nocontent"), container, mentions, 61, anchorMin: anchorMin, anchorMax: anchorMax, spacing: spacing);
				}

				container.Add(new CuiButton { Button = { Command = $"{ClosePostLikesDislikesCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.125 0.05" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawUsers(string title, RusterUser[] users, Action<RusterUser> onUserSelected = null, bool isBackground = false)
			{
				if (onUserSelected != null) OnUserSelected = onUserSelected;

				CloseUsers();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawUsers(title, users, isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? UsersDialogCUI : UsersDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseUsersCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.4 0.1", AnchorMax = "0.6 0.875" }, CursorEnabled = true }, ratioBackground);

				var anchorMin = 0.835f;
				var anchorMax = 0.925f;
				var spacing = 0.07f;

				var likes = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.025 0.075", AnchorMax = "0.965 0.98" }, CursorEnabled = true }, background);
				{
					DrawUsers(title, users, 12, string.Empty, container, likes, 59, anchorMin: anchorMin, anchorMax: anchorMax, spacing: spacing, command: SelectUserUsersCmd);
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseUsersCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.125 0.05" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawUploadAudio(bool isBackground = false)
			{
				CloseUploadAudio();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawUploadAudio(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? UploadAudioDialogCUI : UploadAudioDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.4 0.43", AnchorMax = $"0.6 0.66" }, CursorEnabled = true }, ratioBackground);

				container.Add(new CuiLabel { Text = { Text = GetPhrase("uploadaudio_title"), FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.95" } }, background);

				container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("uploadaudio_c_title")}:", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.025 0.66", AnchorMax = $"1 0.8" } }, background);
				var titleInput = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.115 0.66", AnchorMax = $"0.975 0.8" }, CursorEnabled = true }, background);
				container.Add(new CuiElement
				{
					Parent = titleInput,
					Components =
					{
						new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{NewPostUploadAudioTitleChangeCmd} {User.Id}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 400 },
						new CuiRectTransformComponent { AnchorMin = "0.03 0.15", AnchorMax = "0.96 0.85" }
					}
				});

				container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("uploadaudio_c_url")}:", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.025 0.50", AnchorMax = $"1 0.64" } }, background);
				var urlInput = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.115 0.50", AnchorMax = $"0.975 0.64" }, CursorEnabled = true }, background);
				container.Add(new CuiElement
				{
					Parent = urlInput,
					Components =
					{
						new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{NewPostUploadAudioUrlChangeCmd} {User.Id}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 400 },
						new CuiRectTransformComponent { AnchorMin = "0.03 0.15", AnchorMax = "0.96 0.85" }
					}
				});

				container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("uploadaudio_c_skip")}:", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.025 0.35", AnchorMax = $"1 0.48" } }, background);
				container.Add(new CuiLabel { Text = { Text = $"e.g 1:23\n15", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0.025 0.35", AnchorMax = $"0.975 0.48" } }, background);
				var startTimeInput = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.115 0.35", AnchorMax = $"0.875 0.48" }, CursorEnabled = true }, background);
				container.Add(new CuiElement
				{
					Parent = startTimeInput,
					Components =
					{
						new CuiInputFieldComponent { Text = "", FontSize = 9, Font = DefaultFont, Command = $"{NewPostUploadAudioStartTimeChangeCmd} {User.Id}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 400 },
						new CuiRectTransformComponent { AnchorMin = "0.03 0.15", AnchorMax = "0.96 0.85" }
					}
				});

				container.Add(new CuiLabel { Text = { Text = GetPhrase("uploadaudio_c_notice"), FontSize = 8, Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.05 0.18", AnchorMax = $"0.95 0.35" } }, background);

				var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.025 0.05", AnchorMax = $"0.975 0.18" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{NewPostUploadAudioUploadCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.49 1" }, Text = { Text = $"<b>Upload</b>", Font = DefaultFont, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
				container.Add(new CuiButton { Button = { Command = $"{NewPostUploadAudioCloseCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.51 0", AnchorMax = $"1 1" }, Text = { Text = GetPhrase("cancel"), FontSize = 13, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, buttons);

				CuiHelper.AddUi(Player, container);

				LockPlayer();
			}
			public void DrawHashtagFilter(bool isBackground = false)
			{
				CloseHashtagFilter();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawHashtagFilter(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? HashtagFilterCUI : HashtagFilterGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{HashtagFilterCancelCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, FontSize = 13, Color = "0 0 0 0" } }, ratioBackground);

				var background2 = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.4 0.45", AnchorMax = $"0.6 0.525" }, CursorEnabled = true }, background);
				var titleInput = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.025 0.475", AnchorMax = $"0.975 0.935" }, CursorEnabled = true }, background2);
				container.Add(new CuiElement
				{
					Parent = titleInput,
					Components =
					{
						new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{HashtagFilterChangeCmd} {User.Id} ", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 400 },
						new CuiRectTransformComponent { AnchorMin = "0.03 0.15", AnchorMax = "0.96 0.85" }
					}
				});

				var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.025 0.05", AnchorMax = $"0.975 0.425" }, CursorEnabled = true }, background2);
				container.Add(new CuiButton { Button = { Command = $"{HashtagFilterSubmitCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.49 1" }, Text = { Text = $"<b>{GetPhrase("filter")}</b>", Font = DefaultFont, FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
				container.Add(new CuiButton { Button = { Command = $"{HashtagFilterCancelCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.51 0", AnchorMax = $"1 1" }, Text = { Text = GetPhrase("cancel"), FontSize = 13, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "1 1 1 1" } }, buttons);

				CuiHelper.AddUi(Player, container);

				LockPlayer();
			}
			public void DrawServerViewerBanner()
			{
				if (!Instance.HasPermission(Player, InternetPerm, true)) return;

				var currentServer = ServerViewer.CurrentServer;
				var container = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.81 0.005", AnchorMax = $"0.985 0.185" }, CursorEnabled = true }, Background);

				if (ServerViewer.IsViewing())
				{
					Container.Add(new CuiElement { Parent = container, Components = { Death.GetRawImage(currentServer.BannerUrl, color: "1 1 1 0.5"), new CuiRectTransformComponent { AnchorMin = $"0.025 0.2", AnchorMax = "0.985 0.95" } } });

					Container.Add(new CuiLabel { Text = { Text = $"<b>{currentServer.Name.Truncate(30, "...")}</b>", FontSize = 13, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.055 0", AnchorMax = $"0.95 0.45" } }, container);
					Container.Add(new CuiLabel { Text = { Text = $"<b>{currentServer.IP}:{currentServer.Port}</b>", FontSize = 8, Font = DefaultFont, Align = TextAnchor.UpperLeft, Color = "1 1 1 0.6" }, RectTransform = { AnchorMin = $"0.055 0", AnchorMax = $"0.95 0.33" } }, container);
				}

				if (!Instance.IsInternetOnline)
				{
					var panel = Container.Add(new CuiPanel { Image = { Color = "0.25 0.25 0.25 0.75", }, RectTransform = { AnchorMin = $"0.025 0.01", AnchorMax = $"0.48 0.155" }, CursorEnabled = true }, container);
					Container.Add(new CuiLabel { Text = { Text = GetPhrase("rusternetintoffline"), FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.6" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, panel);
				}
				else
				{
					Container.Add(new CuiButton { Button = { Command = $"{OpenServerViewerCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = $"0.025 0.01", AnchorMax = $"0.35 0.155" }, Text = { Align = TextAnchor.MiddleCenter, Text = GetPhrase("viewservers"), FontSize = 8, Color = "0 0 0 1" } }, container);
					Container.Add(new CuiButton { Button = { Command = currentServer == null ? "" : $"{CloseViewedServerCmd} {User.Id}", Color = currentServer == null ? ArrowButtonColor : CloseButtonColor }, RectTransform = { AnchorMin = $"0.36 0.01", AnchorMax = $"0.53 0.155" }, Text = { Align = TextAnchor.MiddleCenter, Text = GetPhrase("close"), FontSize = 8, Color = currentServer == null ? "1 1 1 1" : "0 0 0 1" } }, container);
				}
			}
			public void DrawServerViewerList(bool isBackground = false)
			{
				CloseServerViewerList();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawServerViewerList(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? ServerViewerListDialogCUI : ServerViewerListDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseServerViewerCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.325 0.15", AnchorMax = "0.625 0.845" }, CursorEnabled = true }, ratioBackground);

				var page = GetPage(69);
				var anchorMin = 0.635f;
				var anchorMax = 0.925f;
				var spacing = 0.32f;
				var servers = Instance.ServerList;
				var blacklistedServers = Instance.ServerBlacklist;
				page.TotalPages = (int)Math.Ceiling((double)servers.Length / ServersPerPage - 1);

				var pageServers = servers.Skip(ServersPerPage * page.CurrentPage).Take(ServersPerPage).ToArray();
				page.Check();

				var serverPanel = container.Add(new CuiPanel { Image = { Color = "0.25 0.25 0.25 0.25", }, RectTransform = { AnchorMin = "0.025 0.075", AnchorMax = "0.975 0.98" }, CursorEnabled = true }, background);
				{
					container.Add(new CuiLabel { Text = { Text = $"<b>{GetPhrase("serverviewer")}</b> {GetPhrase("serverheader", servers.Length.ToString("n0"), blacklistedServers.Length.ToString("n0"))}", FontSize = 13, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"0.98 0.98" } }, serverPanel);

					for (int i = 0; i < pageServers.Length; i++)
					{
						var server = pageServers[i];

						var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.05 {anchorMin}", AnchorMax = $"0.95 {anchorMax}" }, CursorEnabled = true }, serverPanel);
						container.Add(new CuiElement { Parent = panel, Components = { Death.GetRawImage(server.BannerUrl, color: "1 1 1 0.35"), new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
						container.Add(new CuiLabel { Text = { Text = $"<b>{server.Name.Truncate(46, "...")}</b>", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"0.98 0.45" } }, panel);
						container.Add(new CuiLabel { Text = { Text = $"{server.Description?.Replace("\\n", "\n").Truncate(150, "...")}", FontSize = 8, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.02 0.175", AnchorMax = $"0.98 0.325" } }, panel);
						container.Add(new CuiLabel { Text = { Text = $"{(server.IsOfficial ? $"<color=yellow>{GetPhrase("verified")}</color> — " : "")}{server.CurrentPlayers} / {server.MaximumPlayers} — {server.Version} — {server.Checksum.Truncate(6, "")}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"0.98 0.525" } }, panel);

						container.Add(new CuiButton { Button = { Command = $"{OpenPictureViewerCmd} {User.Id} {server.BannerUrl}", Color = "0 0 0 0" }, Text = { Text = string.Empty, Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, panel);

						if (!(server.IP == global::RusterNET.Core.Internet.IP && server.Port == ConVar.Server.port))
							container.Add(new CuiButton { Button = { Command = $"{OpenViewedServerCmd} {User.Id} {server.IP} {server.Port}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.02 0.03", AnchorMax = $"0.13 0.15" }, Text = { Align = TextAnchor.MiddleCenter, FontSize = 8, Text = GetPhrase("view"), Color = "0 0 0 1" } }, panel);

						anchorMin -= spacing;
						anchorMax -= spacing;
					}

					if (servers.Length == 0) container.Add(new CuiLabel { Text = { Text = GetPhrase("noservers"), FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0.05", AnchorMax = $"1 0.925" } }, background);
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseServerViewerCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.125 0.05" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				if (page.TotalPages != -1)
				{
					var b = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.15 0.01", AnchorMax = $"0.75 0.05" }, CursorEnabled = true }, background);
					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, b);
				}

				CuiHelper.AddUi(Player, container);
			}
			public void DrawPictureViewer(string url, float ratioXOffset = 0f, float ratioYOffset = 0f, bool isBackground = false)
			{
				ClosePictureViewer();

				if (User.Configuration.PerformanceMode) isBackground = true;

				var blankImage = string.IsNullOrEmpty(url);
				if (blankImage) url = NoImageUrl;

				if (!isBackground) DrawPictureViewer(url, ratioXOffset, ratioYOffset, isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? PictureViewerDialogCUI : PictureViewerDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{ClosePictureViewerCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = $"{0.15 + ratioXOffset} {0.15 + ratioYOffset}", AnchorMax = $"{0.85 - ratioXOffset} {0.845 - ratioYOffset}" }, CursorEnabled = true }, ratioBackground);

				container.Add(new CuiElement { Parent = background, Components = { Death.GetRawImage(url, color: $"1 1 1 {(blankImage ? 0.15 : 0.7)}"), new CuiRectTransformComponent { AnchorMin = $"0.025 0.05", AnchorMax = "0.98 0.95" } } });
				container.Add(new CuiButton { Button = { Command = $"{ClosePictureViewerCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.96 0.965", AnchorMax = $"0.98 0.99" }, Text = { Text = "X", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawGifPanel(bool isBackground = false)
			{
				CloseBlank();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawGifPanel(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = false
				}, "Overlay", !isBackground ? GifPanelDialogCUI : GifPanelDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = false }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.15 0.15", AnchorMax = "0.85 0.845" }, CursorEnabled = false }, ratioBackground);

				container.Add(new CuiLabel { Text = { Text = $"Press <b>[USE]</b> to close", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.LowerCenter }, RectTransform = { AnchorMin = $"0 0.0125", AnchorMax = $"1 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawGifPanelContent(string url, float bleeding = 0.3f)
			{
				CloseBlankContent();

				var blankImage = string.IsNullOrEmpty(url);
				if (blankImage) url = NoImageUrl;

				var isUrl = url.Contains("http");

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = false }, "Overlay", GifPanelContentCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = false }, background);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.15 0.15", AnchorMax = "0.85 0.845" }, CursorEnabled = false }, ratioBackground);

				var identifier = isUrl ? string.Empty : Instance.ImageLibrary.GetImage(url);
				container.Add(new CuiElement { Parent = background, Components = { isUrl ? Death.GetRawImage(url, color: $"1 1 1 {(blankImage ? 0.15 : 0.9)}", fade: bleeding, sprite: "assets/content/textures/generic/fulltransparent.tga") : new CuiRawImageComponent { Png = identifier, Color = $"1 1 1 {(blankImage ? 0.15 : 0.9)}", FadeIn = bleeding, Sprite = "assets/content/textures/generic/fulltransparent.tga" }, new CuiRectTransformComponent { AnchorMin = $"0.025 0.05", AnchorMax = "0.98 0.95" } } });

				CuiHelper.AddUi(Player, container);
			}
			public void DrawTextEditor(string text = null, Action<string> onSubmit = null, string mainText = null, string secondText = null, Action onCancel = null, Func<string, string, KeyValuePair<bool, string>> canSubmit = null, bool isBackground = false, int characterLimit = 400)
			{
				CloseTextEditor();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawTextEditor(text, onSubmit, mainText, secondText, onCancel, canSubmit, isBackground: true, characterLimit: characterLimit);
				else LockPlayer();

				var noMain = string.IsNullOrEmpty(mainText);
				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? TextEditorDialogCUI : TextEditorDialogGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseTextEditorCmd} {User.Id} 0", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.35 0.35", AnchorMax = "0.65 0.65" }, CursorEnabled = true }, ratioBackground);

				if (!noMain)
				{
					container.Add(new CuiLabel { Text = { Text = mainText, FontSize = 8, Color = "1 1 1 0.6", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"0.985 0.95" } }, background);
					container.Add(new CuiLabel { Text = { Text = $"{text}", FontSize = 12, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"0.985 0.90" } }, background);
				}

				container.Add(new CuiLabel { Text = { Text = secondText, FontSize = 8, Color = "1 1 1 0.6", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"0.985 {(noMain ? 0.95 : 0.65)}" } }, background);
				var titleInput = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.55", }, RectTransform = { AnchorMin = $"0.025 0.15", AnchorMax = $"0.975 {(noMain ? 0.90 : 0.60)}" }, CursorEnabled = true }, background);
				container.Add(new CuiElement
				{
					Parent = titleInput,
					Components =
					{
						new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{TextEditorContentCmd} {User.Id} ", Align = TextAnchor.UpperLeft, Color = "1 1 1 1", CharsLimit = characterLimit },
						new CuiRectTransformComponent { AnchorMin = "0.01 0.15", AnchorMax = "0.99 0.95" }
					}
				});

				container.Add(new CuiButton { Button = { Command = $"{CloseTextEditorCmd} {User.Id} 1", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.025 0.05", AnchorMax = $"0.125 0.12" }, Text = { Text = GetPhrase("submit"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseTextEditorCmd} {User.Id} 0", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.13 0.05", AnchorMax = $"0.23 0.12" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				TextEditorOriginalContent = text;
				TextEditorContent = null;
				OnTextEditorSubmit = onSubmit;
				OnTextEditorCancel = onCancel;
				TextEditorCanSubmit = canSubmit;

				CuiHelper.AddUi(Player, container);
			}
			public void DrawContacts(bool isBackground = false)
			{
				CloseContacts();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawContacts(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel
				{
					Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
					FadeOut = MainFadeout,
					CursorEnabled = true
				}, "Overlay", !isBackground ? ContactsCUI : ContactsGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseContactsCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.875" }, CursorEnabled = true }, ratioBackground);

				var anchorMin = 0.835f;
				var anchorMax = 0.925f;
				var spacing = 0.07f;

				var contacts = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.075", AnchorMax = "0.49 0.98" }, CursorEnabled = true }, background);
				{
					var relationship = RelationshipManager.ServerInstance.GetRelationships(User.Id);
					DrawUsers(GetPhrase("contacts"), relationship.relations.Where(x => x.Key != User.Id && x.Key.IsSteamId()).Select(x => Instance.Data.GetUser(x.Key)).ToArray(), 12, GetPhrase("contacts_none"), container, contacts, 71, anchorMin: anchorMin, anchorMax: anchorMax, spacing: spacing, command: ContactsCommand);
				}

				var search = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.51 0.075", AnchorMax = "0.985 0.98" }, CursorEnabled = true }, background);
				{
					var offset = -0.02f;
					var users = string.IsNullOrEmpty(ContactsFilter) ? Instance.EmptyUserArray : Instance.Data.Users.Where(x => x.GetDisplayName().ToLower().Contains(ContactsFilter) || x.Id.ToString().Contains(ContactsFilter)).ToArray();

					DrawUsers($"{GetPhrase("search")}{(string.IsNullOrEmpty(ContactsFilter) ? "" : $" ({ContactsFilter})")}", users, 12, GetPhrase("filter_none"), container, search, 72, anchorMin: anchorMin + offset, anchorMax: anchorMax + offset, spacing: spacing, command: ContactsCommand);

					var searchBar = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.95" }, CursorEnabled = true }, search);
					container.Add(new CuiLabel { Text = { Text = $"{GetPhrase("filter")}: ", FontSize = 10, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, searchBar);
					container.Add(new CuiElement
					{
						Parent = searchBar,
						Components =
						{
							new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{ChangeContactsFilterCmd} {User.Id} ", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 56 },
							new CuiRectTransformComponent { AnchorMin = "0.1 0", AnchorMax = "1 1" }
						}
					});
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseContactsCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.125 0.05" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);

				LockPlayer();
			}
			public void DrawUserPhotoList(RusterLicensedItem.ItemTypes type, Action<string> onSelected = null, Action onClear = null, bool isBackground = false)
			{
				if (onSelected != null) OnUserPhotoSelected = onSelected;
				if (onClear != null) OnUserPhotoClear = onClear;

				CloseUserPhotoList();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawUserPhotoList(type: type, isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? UserPhotoListCUI : UserPhotoListGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseUserPhotoListCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.6", }, RectTransform = { AnchorMin = "0.2 0.3", AnchorMax = "0.8 0.7" }, CursorEnabled = true }, ratioBackground);

				var step1 = Instance.Data.GetLicensedItems(User).Select(x => Instance.Config.License.GetLicense(x));
				var step2 = step1.Where(x => x.ItemType == type);
				var step3 = step2.Select(x => x.Value).ToArray();

				var userPhotos = Facepunch.Pool.GetList<string>();
				userPhotos.Add("Clear");
				userPhotos.Add("Store");

				var pageIndex = 0;
				var title = string.Empty;
				var ratioOffset = 1f;
				switch (type)
				{
					case RusterLicensedItem.ItemTypes.Avatar:
						pageIndex = 85;
						title = "Avatars";
						userPhotos.AddRange(GetDefaultAvatars());
						ratioOffset = 1f;
						break;

					case RusterLicensedItem.ItemTypes.Banner:
						pageIndex = 86;
						title = "Banners";
						userPhotos.AddRange(GetDefaultBanners());
						ratioOffset = 0.65f;
						break;

					case RusterLicensedItem.ItemTypes.Frame:
						pageIndex = 87;
						title = "Frames";
						ratioOffset = 1f;
						break;
				}

				userPhotos.AddRange(step3);

				var page = GetPage(pageIndex);
				page.TotalPages = (int)Math.Ceiling((double)userPhotos.Count / 4 - 1);
				page.Check();
				var pageAvatars = userPhotos.Skip(page.CurrentPage * 4).Take(4).ToArray();

				container.Add(new CuiLabel { Text = { Text = $"<b>{title}</b> {userPhotos.Count - 2:n0}", FontSize = 20, Color = "1 1 1 0.2", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 0.95" } }, background);

				var offset = 0f;
				var spacing = 180f;
				for (int i = 0; i < 4; i++)
				{
					if (i > pageAvatars.Length - 1) continue;

					var isUrl = pageAvatars[i].Contains("http");
					var isEmpty = !isUrl || string.IsNullOrEmpty(pageAvatars[i]);
					var button = container.Add(new CuiButton { Button = { Command = $"{SelectUserPhotoListCmd} {User.Id} {pageAvatars[i]}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.26 0.8", OffsetMin = $"{offset} 0", OffsetMax = $"{offset} 0" }, Text = { Text = !isEmpty ? "" : $"<b>{pageAvatars[i].ToUpper()}</b>", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.1" } }, background);
					if (!isEmpty) container.Add(new CuiElement { Parent = button, Components = { Death.GetRawImage(pageAvatars[i], color: "1 1 1 0.8"), new CuiRectTransformComponent { AnchorMin = $"0 {1f - ratioOffset}", AnchorMax = $"1 {ratioOffset}" } } });
					if (Instance.Data.HasLicense(User, pageAvatars[i]))
					{
						var exclusiveBadge = container.Add(new CuiPanel { Image = { Color = NormalButtonColor, }, RectTransform = { AnchorMin = "0.7 0.9", AnchorMax = "0.98 0.98" }, CursorEnabled = true }, button);
						container.Add(new CuiLabel { Text = { Text = $"EXCLUSIVE", FontSize = 8, Color = "0 0 0 1", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, exclusiveBadge);
					}

					offset += spacing;
				}

				container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.41 0.05", AnchorMax = $"0.44 0.15" }, Text = { Text = "◀", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseUserPhotoListCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.45 0.05", AnchorMax = $"0.55 0.15" }, Text = { Text = "Close", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" } }, background);
				container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.56 0.05", AnchorMax = $"0.59 0.15" }, Text = { Text = "▶", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);

				Facepunch.Pool.FreeList(ref userPhotos);
				pageAvatars = null;
				step1 = null;
				step2 = null;
				step3 = null;

				CuiHelper.AddUi(Player, container);
			}

			public void DrawUserSettings(bool isBackground = false)
			{
				CloseUserSettings();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawUserSettings(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? UserSettingsCUI : UserSettingsGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseUserSettingsCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.85 0.95" }, CursorEnabled = true }, ratioBackground);

				var user = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.075", AnchorMax = "0.49 0.98" }, CursorEnabled = true }, background);
				{
					container.Add(new CuiLabel { Text = { Text = $"<b>User Settings</b>", FontSize = 20, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.98" } }, user);
					container.Add(new CuiLabel { Text = { Text = $"All configurations available for your user account.", FontSize = 8, Color = "1 1 1 0.2", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.94" } }, user);

					var setting = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.87", AnchorMax = "0.99 0.92" }, CursorEnabled = true }, user);
					var spacing = -32f;
					var offset = Math.Abs(spacing);
					var page = GetPage(81);
					page.TotalPages = 1;

					DrawUserSettingPaging(page, setting, offset += spacing, container);

					switch (page.CurrentPage)
					{
						case 0:
							DrawUserSetting(SettingTypes.Nickname, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.Language, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.PrivacyMode, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.Ratio, setting, offset += spacing, container);

							DrawUserSettingCategory($"Shopping", setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.PaymentMethod, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.Gift, setting, offset += spacing, container);

							DrawUserSettingCategory($"Notifications", setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.PushNotifications, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.FriendsNotifications, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.RustPlusNotifications, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.DMNotifications, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.ChatNotifications, setting, offset += spacing, container);

							if (User.IsDeveloper())
							{
								DrawUserSettingCategory($"Developer", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.DeveloperBypass, setting, offset += spacing, container);
							}
							break;

						case 1:
							DrawUserSettingCategory($"Pinning", setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.AudioPlayer_Pin, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.NotificationTray_Pin, setting, offset += spacing, container);

							DrawUserSettingCategory($"Colors", setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.FeedBackgroundColor, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.FeedTitleColor, setting, offset += spacing, container);

							DrawUserSettingCategory($"GIFs", setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.GifDuration, setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.GifBoomerang, setting, offset += spacing, container);

							DrawUserSettingCategory($"Settings", setting, offset += spacing, container);
							DrawUserSetting(SettingTypes.PerformanceMode, setting, offset += spacing, container);
							break;
					}
				}

				if (User.IsDeveloper() || User.IsAdmin())
				{
					var admin = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.5 0.075", AnchorMax = "1 0.98" }, CursorEnabled = true }, background);
					{
						container.Add(new CuiLabel { Text = { Text = $"<b>Admin Settings</b>", FontSize = 20, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.98" } }, admin);
						container.Add(new CuiLabel { Text = { Text = $"Administrative settings. They affect most users that use Ruster.NET and the way overall features are reachable to individuals.", FontSize = 8, Color = "1 1 1 0.2", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 0.94" } }, admin);

						var setting = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.01 0.87", AnchorMax = "0.99 0.92" }, CursorEnabled = true }, admin);
						var spacing = -32f;
						var offset = Math.Abs(spacing);
						var page = GetPage(82);
						page.TotalPages = 3;

						DrawUserSettingPaging(page, setting, offset += spacing, container);

						switch (page.CurrentPage)
						{
							case 0:
								DrawUserSetting(SettingTypes.Version, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminTax, setting, offset += spacing, container);

								DrawUserSettingCategory($"Features", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminMarketplace, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminFlipbook, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminLocation, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAddPhoto, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAddCassette, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminRecordMemo, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAutoHolidayMode, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAddGif, setting, offset += spacing, container);

								DrawUserSettingCategory($"Marketplace", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminRefundOnDelete, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminRefundOnExpiredAdvert, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminMinMarketPrice, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminMaxMarketPrice, setting, offset += spacing, container);

								DrawUserSettingCategory($"Stories", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminSimultaneousStoriesPosts, setting, offset += spacing, container);

								break;

							case 1:
								DrawUserSettingCategory($"Sounds", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminPlayStartupSound, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminPlayBeepSound, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminPlayLikesSound, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminPlayDislikeSound, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminPlayVibrations, setting, offset += spacing, container);

								DrawUserSettingCategory($"DMs", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminMustBeFriendsToDM, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAutoTeamGroups, setting, offset += spacing, container);

								DrawUserSettingCategory($"Colors", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdminNameColor, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminModeratorNameColor, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminDeveloperNameColor, setting, offset += spacing, container);

								DrawUserSettingCategory($"Feeds", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.CommunityBackgroundColor, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.CommunityTitleColor, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.MarketplaceBackgroundColor, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.MarketplaceTitleColor, setting, offset += spacing, container);

								break;

							case 2:
								DrawUserSettingCategory($"Lottery", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminStockValue, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminStartLotteryEvent, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminLotteryEventDuration, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminLotteryEventMinimumTax, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminLotteryEventMinimumTickets, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminLotteryEventMinimumPlayers, setting, offset += spacing, container);

								DrawUserSettingCategory($"Advert Coupons", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvert24hCoupon, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvert1wCoupon, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvertShortFlipbookCoupon, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvertMediumFlipbookCoupon, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvertLongFlipbookCoupon, setting, offset += spacing, container);

								DrawUserSettingCategory($"Advert Prices", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvert24hPrice, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvertShortFlipbookPrice, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvertMediumFlipbookPrice, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminAdvertLongFlipbookPrice, setting, offset += spacing, container);
								break;

							case 3:
								DrawUserSetting(SettingTypes.AdminFlipbookResetPrice, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminTradePrice, setting, offset += spacing, container);

								DrawUserSettingCategory($"Miscellaneous", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminDisableBlur, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminBackgroundOpacity, setting, offset += spacing, container);

								DrawUserSettingCategory($"Photographs", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.AdminImgurClientId, setting, offset += spacing, container);

								DrawUserSettingCategory($"System", setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.UpdateLanguages, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.Save, setting, offset += spacing, container);
								DrawUserSetting(SettingTypes.Reload, setting, offset += spacing, container);
								break;
						}
					}
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseUserSettingsCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.125 0.05" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);

				LockPlayer();
			}
			public void DrawColorPicker(bool isBackground = false, Action<string, string> onColorPicked = null, Action onCancel = null)
			{
				if (onColorPicked != null) OnColorPicked = onColorPicked;
				if (onCancel != null) OnColorCancel = onCancel;

				CloseColorPicker();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawColorPicker(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? ColorPickerCUI : ColorPickerGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseColorPickerCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.85 0.95" }, CursorEnabled = true }, ratioBackground);

				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = "0.325 0.25", AnchorMax = "0.675 0.75" }, CursorEnabled = true }, background);
				{
					var scale = 20f;
					var offset = scale * 0.770f;
					var total = (scale * 2) - 8f;

					var topRightColor = Color.blue;
					var bottomRightColor = Color.green;
					var topLeftColor = Color.red;
					var bottomLeftColor = Color.yellow;

					for (float y = 0; y < scale; y += 1f)
					{
						var heightColor = Color.Lerp(topRightColor, bottomRightColor, y.Scale(0f, scale, 0f, 1f));

						for (float x = 0; x < scale; x += 1f)
						{
							var widthColor = Color.Lerp(topLeftColor, bottomLeftColor, (x + y).Scale(0f, total, 0f, 1f));
							var color = Color.Lerp(widthColor, heightColor, x.Scale(0f, scale, 0f, 1f)) * ColorBrightness;
							DrawColor(scale, color, panel, offset * x, -(offset * y), container);
						}
					}

					//
					// Brightness
					//
					for (float x = 0; x < scale; x += 1f)
					{
						var color = Color.Lerp(Color.black, Color.white, x.Scale(0f, scale, 0f, 1f));
						DrawColor(scale, color, panel, offset * x, -(offset * (scale + 1f)), container, mode: "brightness");
					}

					//
					// Saturation
					//
					for (float y = 0; y < scale; y += 1f)
					{
						var color = Color.Lerp(Color.white, Color.black, y.Scale(0f, scale, 0f, 1f));
						DrawColor(scale, color, panel, offset * (scale + 1f), -(offset * y), container);
					}
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseColorPickerCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = $"0.125 0.05" }, Text = { Text = GetPhrase("cancel"), FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, background);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawCouponEditor(Action<RusterCoupon> onCouponEditorSaved = null, Action onCouponEditorClear = null, bool isBackground = false)
			{
				if (onCouponEditorSaved != null) OnCouponEditorSaved = onCouponEditorSaved;
				if (onCouponEditorClear != null) OnCouponEditorClear = onCouponEditorClear;

				CloseCouponEditor();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawCouponEditor(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? CouponEditorCUI : CouponEditorGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseCouponEditorCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.85 0.95" }, CursorEnabled = true }, ratioBackground);

				var offset = 0f;
				var spacing = 50f;
				DrawCouponEditorOption("Code", EditingCoupon.Code, $"{SetOptionCouponEditorCmd} {User.Id} code", "0.35 0.53", "0.65 0.6", offset, container, background);
				DrawCouponEditorOption("Discount (%)", $"{EditingCoupon.Discount}%", $"{SetOptionCouponEditorCmd} {User.Id} discount", "0.35 0.53", "0.65 0.6", offset -= spacing, container, background);
				DrawCouponEditorOption("Maximum Uses", EditingCoupon.MaximumUses <= 0 ? "Unlimited" : EditingCoupon.MaximumUses.ToString("n0"), $"{SetOptionCouponEditorCmd} {User.Id} maxuses", "0.35 0.53", "0.65 0.6", offset -= spacing, container, background);
				var buttons = container.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.35 0.55", AnchorMax = "0.65 0.6", OffsetMin = $"0 {offset -= spacing}", OffsetMax = $"0 {offset}" } }, background);
				{
					container.Add(new CuiButton { Button = { Command = $"{SaveCouponEditorCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.48 1" }, Text = { Text = "Save", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 12 } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{CloseCouponEditorCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.52 0", AnchorMax = $"1 1" }, Text = { Text = "Cancel", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 12 } }, buttons);
				}
				var buttons2 = container.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.35 0.55", AnchorMax = "0.65 0.6", OffsetMin = $"0 {offset -= spacing / 1.3f}", OffsetMax = $"0 {offset}" } }, background);
				{
					container.Add(new CuiButton { Button = { Command = $"{ClearCouponEditorCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = $"Clear Coupon ({EditingCoupon.Uses.Count:n0} {EditingCoupon.Uses.Count.Plural("use", "uses")})", Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.4", FontSize = 12 } }, buttons2);
				}

				CuiHelper.AddUi(Player, container);

				LockPlayer();
			}
			private void DrawCouponEditorOption(string title, string previousValue, string command, string anchorMin, string anchorMax, float yOffset, CuiElementContainer container, string parent)
			{
				var code = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.85", }, RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = $"0 {yOffset}", OffsetMax = $"0 {yOffset}" }, CursorEnabled = true }, parent);
				{
					container.Add(new CuiLabel { Text = { Text = $"<b>{title}</b>", FontSize = 10, Color = "1 1 1 0.6", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.95 0.92" } }, code);
					if (!string.IsNullOrEmpty(previousValue)) container.Add(new CuiLabel { Text = { Text = $"{previousValue}", FontSize = 10, Color = "1 1 1 0.6", Font = DefaultFont, Align = TextAnchor.UpperRight }, RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.95 0.92" } }, code);

					container.Add(new CuiElement
					{
						Parent = code,
						Components =
						{
							new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{command} ", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 20 },
							new CuiRectTransformComponent { AnchorMin = "0.03 0.03", AnchorMax = "0.96 0.75" }
						}
					});
				}
			}

			public void DrawCouponList(bool isBackground = false)
			{
				CloseCouponList();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawCouponList(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? CouponListCUI : CouponListGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseCouponListCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.85 0.95" }, CursorEnabled = true }, ratioBackground);

				var coupons = User.Configuration.Coupons;
				var page = GetPage(83);
				page.TotalPages = (int)Math.Ceiling((double)coupons.Count / 7 - 1);
				var pageCoupons = coupons.Skip(page.CurrentPage * 7).Take(7).ToArray();

				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.3 0.3", AnchorMax = $"0.6 0.7" }, CursorEnabled = true }, background);
				container.Add(new CuiLabel { Text = { Text = $"<b>Coupons</b> {coupons.Count:n0}", FontSize = 20, Color = "1 1 1 0.2", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, panel);
				container.Add(new CuiButton { Button = { Command = $"{AddCouponListCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.875 0.92", AnchorMax = $"1 1" }, Text = { Text = "Add", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12 } }, panel);

				var topBar = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5" }, RectTransform = { AnchorMin = $"0 0.81", AnchorMax = $"1 0.9" }, CursorEnabled = true }, panel);
				{
					container.Add(new CuiLabel { Text = { Text = $"#", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = $"Code", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.1 0", AnchorMax = $"1 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = $"Actions", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1" } }, topBar);
				}

				var spacing = 24f;
				var offset = 0f;

				for (int i = 0; i < 7; i++)
				{
					var bar = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5" }, RectTransform = { AnchorMin = $"0 0.81", AnchorMax = $"1 0.9", OffsetMin = $"0 {offset -= spacing}", OffsetMax = $"0 {offset}" }, CursorEnabled = true }, panel);
					{
						var coupon = i <= pageCoupons.Length - 1 ? pageCoupons[i] : null;

						container.Add(new CuiLabel { Text = { Text = $"{i + 1 + (page.CurrentPage * 7):n0}", FontSize = 10, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1" } }, bar);
						container.Add(new CuiLabel { Text = { Text = coupon == null ? "" : coupon, FontSize = 10, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.1 0", AnchorMax = $"1 1" } }, bar);
						if (coupon != null) container.Add(new CuiButton { Button = { Command = $"{RemoveCouponListCmd} {User.Id} {i + (page.CurrentPage * 7)}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.875 0.2", AnchorMax = $"1 0.8", OffsetMin = "-5 0", OffsetMax = "-5 0" }, Text = { Text = "Remove", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, bar);
					}
				}

				if (page.TotalPages > -1)
				{
					var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.025 0.01", AnchorMax = $"0.85 0.1" }, CursorEnabled = true }, panel);
					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseCouponListCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.825 0.01", AnchorMax = $"1 0.1" }, Text = { Text = "Close", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, panel);

				CuiHelper.AddUi(Player, container);
			}
			public void DrawTransactionList(bool isBackground = false)
			{
				CloseTransactionList();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawTransactionList(isBackground: true);

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? TransactionListCUI : TransactionListGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseTransactionListCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.85 0.95" }, CursorEnabled = true }, ratioBackground);

				var transactions = IsViewingPurchases ? Instance.Data.GetPurchases(User) : Instance.Data.GetSales(User);
				var page = GetPage(84);
				page.TotalPages = (int)Math.Ceiling((double)transactions.Count / 7 - 1);
				var pageTransactions = transactions.Skip(page.CurrentPage * 7).Take(7).ToArray();

				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = $"0.15 0.3", AnchorMax = $"0.85 0.7" }, CursorEnabled = true }, background);
				container.Add(new CuiLabel { Text = { Text = IsViewingPurchases ? $"<b>Purchases</b> {transactions.Count:n0}" : $"<b>Sales</b> {transactions.Count:n0}", FontSize = 20, Color = "1 1 1 0.2", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, panel);
				container.Add(new CuiButton { Button = { Command = $"{SwitchViewTransactionListCmd} {User.Id} 0", Color = IsViewingPurchases ? ArrowButtonColor : NormalButtonColor }, RectTransform = { AnchorMin = "0.82 0.92", AnchorMax = $"0.885 1" }, Text = { Text = "Sales", Color = IsViewingPurchases ? "1 1 1 1" : "0 0 0 1", FontSize = 10, Align = TextAnchor.MiddleCenter } }, panel);
				container.Add(new CuiButton { Button = { Command = $"{SwitchViewTransactionListCmd} {User.Id} 1", Color = !IsViewingPurchases ? ArrowButtonColor : NormalButtonColor }, RectTransform = { AnchorMin = "0.89 0.92", AnchorMax = $"1 1" }, Text = { Text = "Purchases", Color = !IsViewingPurchases ? "1 1 1 1" : "0 0 0 1", FontSize = 10, Align = TextAnchor.MiddleCenter } }, panel);

				var topBar = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5" }, RectTransform = { AnchorMin = $"0 0.81", AnchorMax = $"1 0.9" }, CursorEnabled = true }, panel);
				{
					container.Add(new CuiLabel { Text = { Text = $"#", FontSize = 10, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.015 0", AnchorMax = $"1 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = IsViewingPurchases ? "Seller" : "Buyer", FontSize = 10, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.04 0", AnchorMax = $"1 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = $"Item", FontSize = 10, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.52 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = $"Coupon", FontSize = 10, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.7 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = $"Price", FontSize = 10, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.8 1" } }, topBar);
					container.Add(new CuiLabel { Text = { Text = $"Date", FontSize = 10, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1" } }, topBar);
				}

				var spacing = 25f;
				var offset = 0f;

				for (int i = 0; i < 7; i++)
				{
					var bar = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5" }, RectTransform = { AnchorMin = $"0 0.81", AnchorMax = $"1 0.9", OffsetMin = $"0 {offset -= spacing}", OffsetMax = $"0 {offset}" }, CursorEnabled = true }, panel);
					{
						var transaction = i <= pageTransactions.Length - 1 ? pageTransactions[i] : null;
						container.Add(new CuiLabel { Text = { Text = $"{i + 1 + (page.CurrentPage * 7):n0}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.015 0", AnchorMax = $"1 1" } }, bar);

						if (transaction != null)
						{
							var seller = Instance.Data.GetUser(transaction.UserId);
							var item = ItemManager.FindItemDefinition(transaction.Item);
							container.Add(new CuiLabel { Text = { Text = $"{seller.GetDisplayName()} {seller.GetOnlineIcon()}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.04 0", AnchorMax = $"1 1" } }, bar);
							container.Add(new CuiLabel { Text = { Text = $"{transaction.Amount} x {(string.IsNullOrEmpty(transaction.Name) ? GetPhrase(item) : transaction.Name)}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.52 1" } }, bar);
							container.Add(new CuiLabel { Text = { Text = $"{(string.IsNullOrEmpty(transaction.Coupon) ? "None" : $"<color=green>-{transaction.Discount}%</color> — {transaction.Coupon}")}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.7 1" } }, bar);
							container.Add(new CuiLabel { Text = { Text = $"{(transaction.Price == 0 ? "Free" : Instance.Config.Currency.GetValueName(this, transaction.Price))}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.8 1" } }, bar);
							container.Add(new CuiLabel { Text = { Text = $"{new DateTick(transaction.Ticks).Date.DateFormat2}", FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleRight }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1" } }, bar);
							container.Add(new CuiButton { Button = { Command = $"{RemoveTransactionListCmd} {User.Id} {i + (page.CurrentPage * 7)} {IsViewingPurchases}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.98 0.2", AnchorMax = $"1 0.8", OffsetMin = "-2 0", OffsetMax = "-2 0" }, Text = { Text = "x", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, bar);
						}
					}
				}

				if (page.TotalPages > -1)
				{
					var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.025 0.01", AnchorMax = $"0.45 0.075" }, CursorEnabled = true }, panel);
					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
				}
				container.Add(new CuiButton { Button = { Command = $"{CloseTransactionListCmd} {User.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.9 0.01", AnchorMax = $"1 0.075" }, Text = { Text = "Close", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, panel);

				CuiHelper.AddUi(Player, container);
			}

			public void DrawModal(bool isBackground = false)
			{
				CloseModal();

				if (Modal == null) return;

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) DrawModal(isBackground: true);

				var fields = Modal.Fields;
				var fieldsPerPage = 5;
				var page = GetPage(200);
				page.TotalPages = (int)Math.Ceiling((double)fields.Count / fieldsPerPage - 1);
				page.Check();
				var pageFields = fields.Skip(page.CurrentPage * fieldsPerPage).Take(fieldsPerPage).ToArray();

				var container = new CuiElementContainer();
				var background = container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(Instance.Config.Look.DisableBlur ? 0 : !isBackground ? 0.2 : 0.5)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, FadeOut = MainFadeout, CursorEnabled = true }, "Overlay", !isBackground ? ModalCUI : ModalGhostCUI);
				var ratioBackground = container.Add(new CuiPanel { Image = { Color = Instance.Config.Look.DisableBlur ? $"0.15 0.15 0.15 {Instance.Config.Look.BackgroundOpacity}" : "0 0 0 0", }, RectTransform = { AnchorMin = $"{User.Configuration.GetMinRatio()} 0", AnchorMax = $"{User.Configuration.GetMaxRatio()} 1" }, CursorEnabled = true }, background);
				container.Add(new CuiButton { Button = { Command = $"{CloseModalCmd} {User.Id}", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, ratioBackground);
				background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0.3 0", AnchorMax = "0.7 1" }, CursorEnabled = true }, ratioBackground);

				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.75", }, RectTransform = { AnchorMin = $"0.15 0.3", AnchorMax = $"0.85 0.7" }, CursorEnabled = true }, background);
				container.Add(new CuiLabel { Text = { Text = $"<b>{Modal.Title}</b>", FontSize = 20, Color = "1 1 1 0.9", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.04 0", AnchorMax = $"1 0.965" } }, panel);
				container.Add(new CuiLabel { Text = { Text = $"{Modal.Description}", FontSize = 13, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.05 0", AnchorMax = $"1 0.89" } }, panel);

				var push = 1f;
				var fieldsContent = container.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = $"0.04 0.15", AnchorMax = $"0.96 0.8" }, CursorEnabled = true }, panel);
				var height = 1f.Scale(0f, 5, 0f, 1f);

				for (int i = 0; i < 5; i++)
				{
					var field = i > pageFields.Length - 1 ? new KeyValuePair<string, RusterModal.RusterField>(null, null) : pageFields[i];
					var bar = container.Add(new CuiPanel { Image = { Color = field.Value == null ? "0 0 0 0.35" : "0 0 0 0.65" }, RectTransform = { AnchorMin = $"0 {push - height}", AnchorMax = $"1 {push}" }, CursorEnabled = true }, fieldsContent);
					{
						if (field.Value != null)
						{
							container.Add(new CuiLabel { Text = { Text = $"{(field.Value.IsRequired ? "<color=red>*</color> " : "")}<b>{field.Value.Title}</b>{(string.IsNullOrEmpty(field.Value.Description) ? "" : $"\n<size=10>{field.Value.Description}</size>")}", FontSize = 13, Color = "1 1 1 0.85", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1" } }, bar);

							switch (field.Value.FieldType)
							{
								case RusterModal.RusterField.FieldTypes.String:
								case RusterModal.RusterField.FieldTypes.Number:
									var textInput = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0.95", }, RectTransform = { AnchorMin = "0.7 0.2", AnchorMax = "0.92 0.8" }, CursorEnabled = true }, bar);
									container.Add(new CuiLabel { Text = { Text = $"<i>{field.Value.Value}</i>", FontSize = 14, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = "0.03 0", AnchorMax = "1 1" } }, textInput);
									container.Add(new CuiElement
									{
										Parent = textInput,
										Components =
										{
											new CuiInputFieldComponent { Text = "", FontSize = 14, Font = DefaultFont, Command = $"{ValueModalCmd} {User.Id} {field.Key}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = PostLength },
											new CuiRectTransformComponent { AnchorMin = "0.03 0", AnchorMax = "1 1" }
										}
									});
									break;

								case RusterModal.RusterField.FieldTypes.Toggle:
									var value = field.Value.Value.ToBool();
									container.Add(new CuiButton { Button = { Command = $"{ValueModalCmd} {User.Id} {field.Key}", Color = value ? NormalButtonColor : CloseButtonColor }, RectTransform = { AnchorMin = "0.7 0.2", AnchorMax = "0.92 0.8" }, Text = { Text = value ? "YES" : "NO", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 10 } }, bar);
									break;
							}

							container.Add(new CuiButton { Button = { Command = $"{ResetValueModalCmd} {User.Id} {field.Key}", Color = "1 0.3 0 0.5" }, RectTransform = { AnchorMin = "0.92 0.2", AnchorMax = "0.98 0.8" }, Text = { Text = "<b>R</b>", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, bar);
						}

						push -= height + 0.015f;
					}
				}

				var buttonsOffset = 3f;

				if (page.TotalPages > -1)
				{
					var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.04 0.01", AnchorMax = $"0.465 0.075", OffsetMin = $"0 {buttonsOffset}", OffsetMax = $"0 {buttonsOffset}" }, CursorEnabled = true }, panel);
					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
				}

				container.Add(new CuiButton { Button = { Command = $"{CloseModalCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.75 0.01", AnchorMax = $"0.85 0.075", OffsetMin = $"0 {buttonsOffset}", OffsetMax = $"0 {buttonsOffset}" }, Text = { Text = "Close", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, panel);
				container.Add(new CuiButton { Button = { Command = $"{SubmitModalCmd} {User.Id}", Color = NormalButtonColor }, RectTransform = { AnchorMin = "0.86 0.01", AnchorMax = $"0.96 0.075", OffsetMin = $"0 {buttonsOffset}", OffsetMax = $"0 {buttonsOffset}" }, Text = { Text = "Submit", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8 } }, panel);

				CuiHelper.AddUi(Player, container);

				LockPlayer();
			}

			public enum SettingTypes
			{
				DeveloperBypass,

				Nickname,
				Language,
				PerformanceMode,
				PrivacyMode,
				AudioPlayer_Pin,
				NotificationTray_Pin,
				Ratio,
				PushNotifications,
				FriendsNotifications,
				RustPlusNotifications,
				DMNotifications,
				ChatNotifications,
				FeedBackgroundColor,
				FeedTitleColor,
				GifDuration,
				GifBoomerang,
				PaymentMethod,
				Gift,

				AdminStockValue,
				AdminStartLotteryEvent,
				AdminLotteryEventDuration,
				AdminLotteryEventMinimumTax,
				AdminLotteryEventMinimumTickets,
				AdminLotteryEventMinimumPlayers,
				AdminTax,
				AdminMarketplace,
				AdminLocation,
				AdminAddPhoto,
				AdminFlipbook,
				AdminAddCassette,
				AdminRecordMemo,
				AdminAddGif,
				AdminAutoHolidayMode,
				AdminRefundOnDelete,
				AdminRefundOnExpiredAdvert,
				AdminMinMarketPrice,
				AdminMaxMarketPrice,
				AdminSimultaneousStoriesPosts,
				AdminPlayStartupSound,
				AdminPlayBeepSound,
				AdminPlayLikesSound,
				AdminPlayDislikeSound,
				AdminPlayVibrations,
				AdminMustBeFriendsToDM,
				AdminAutoTeamGroups,
				AdminDisableBlur,
				AdminBackgroundOpacity,
				AdminAdminNameColor,
				AdminModeratorNameColor,
				AdminDeveloperNameColor,
				AdminAdvert24hCoupon,
				AdminAdvert1wCoupon,
				AdminAdvertShortFlipbookCoupon,
				AdminAdvertMediumFlipbookCoupon,
				AdminAdvertLongFlipbookCoupon,
				AdminAdvert24hPrice,
				AdminAdvertShortFlipbookPrice,
				AdminAdvertMediumFlipbookPrice,
				AdminAdvertLongFlipbookPrice,
				AdminFlipbookResetPrice,
				AdminTradePrice,
				AdminImgurClientId,

				CommunityBackgroundColor,
				CommunityTitleColor,
				MarketplaceBackgroundColor,
				MarketplaceTitleColor,

				Version,
				UpdateLanguages,
				Save,
				Reload
			}
			public enum OptionTypes
			{
				Button,
				Input,
				Color,
				Enum,
				Blank
			}

			public void DrawColor(float scale, Color color, string parent, float xOffset, float yOffset, CuiElementContainer container, string mode = "color")
			{
				var size = Humanlights.Extensions.MathEx.Scale(1f, 0, scale, 0f, 1f);
				var min = $"0 {size * (scale - 1f)}";
				var max = $"{1f - (size * (scale - 1f))} 1";

				container.Add(new CuiButton { Button = { Command = $"{PickColorPickerCmd} {User.Id} {mode} {ColorUtility.ToHtmlStringRGBA(color)} {color.r} {color.g} {color.b}", Color = $"{color.r} {color.g} {color.b} 1" }, RectTransform = { AnchorMin = min, AnchorMax = max, OffsetMin = $"{xOffset} {yOffset}", OffsetMax = $"{xOffset} {yOffset}" }, Text = { Text = string.Empty, Color = "0 0 0 0" } }, parent);
			}
			public void DrawUserSetting(SettingTypes setting, string parent, float yOffset, CuiElementContainer container)
			{
				var name = string.Empty;
				var value = string.Empty;
				var option = OptionTypes.Button;

				#region User

				switch (setting)
				{
					case SettingTypes.Nickname:
						name = $"Nickname";
						value = User.CustomDisplayName;
						option = OptionTypes.Input;
						break;

					case SettingTypes.Language:
						name = $"Language";
						value = Instance.Config.Localisation.GetLanguage(User.Configuration.Language).Name;
						option = OptionTypes.Button;
						break;

					case SettingTypes.PerformanceMode:
						name = "Performance Mode";
						value = User.Configuration.PerformanceMode.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.PrivacyMode:
						name = "Privacy Mode";
						value = User.Configuration.PrivacyMode.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.Ratio:
						name = $"Ratio";
						value = User.Configuration.Ratio.ToString();
						option = OptionTypes.Input;
						break;


					case SettingTypes.AudioPlayer_Pin:
						name = "Pin Audio Player";
						value = User.Configuration.PinAudioPlayer.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.NotificationTray_Pin:
						name = "Pin Notification Tray";
						value = User.Configuration.PinNotificationTray.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.PaymentMethod:
						name = "Payment Method";
						value = User.Configuration.PaymentMethod.ToString();
						option = OptionTypes.Enum;
						break;

					case SettingTypes.Gift:
						name = "Gift Target";
						value = User.Configuration.GiftTarget == null ? "None" : User.Configuration.GiftTarget.GetDisplayName(true, User);
						option = OptionTypes.Button;
						break;


					case SettingTypes.PushNotifications:
						name = "Push Notifications";
						value = User.Configuration.PushNotifications.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.FriendsNotifications:
						name = "Friends Notifications";
						value = User.Configuration.FriendsNotifications.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.RustPlusNotifications:
						name = "Rust+ Notifications";
						value = User.Configuration.RustPlusNotifications.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.DMNotifications:
						name = "DM Notifications";
						value = User.Configuration.DMNotifications.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.ChatNotifications:
						name = "Chat Notifications";
						value = User.Configuration.ChatNotifications.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.GifDuration:
						name = $"GIF Duration";
						value = User.Configuration.GifDuration.ToString();
						option = OptionTypes.Input;
						break;

					case SettingTypes.GifBoomerang:
						name = "GIF Boomerang";
						value = User.Configuration.GifBoomerang.ToString();
						option = OptionTypes.Button;
						break;
				}

				#endregion

				#region Admin

				switch (setting)
				{
					case SettingTypes.DeveloperBypass:
						name = $"<color=red>Developer Bypass</color>";
						value = User.Configuration.DeveloperBypass.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminTax:
						name = $"Tax";
						value = Instance.Config.Tax.Value.ToString();
						option = OptionTypes.Input;
						break;

					case SettingTypes.AdminMinMarketPrice:
						name = $"Minimum Price";
						value = Instance.Config.Marketplace.MinimumPrice.ToString();
						option = OptionTypes.Input;
						break;

					case SettingTypes.AdminMaxMarketPrice:
						name = $"Maximum Price";
						value = Instance.Config.Marketplace.MaximumPrice.ToString();
						option = OptionTypes.Input;
						break;

					case SettingTypes.AdminRefundOnDelete:
						name = $"Refund On Delete";
						value = Instance.Config.Marketplace.RefundOnDelete.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminRefundOnExpiredAdvert:
						name = $"Refund On Expired Advert";
						value = Instance.Config.Marketplace.RefundOnExpiredAdvert.ToString();
						option = OptionTypes.Button;
						break;


					case SettingTypes.AdminMarketplace:
						name = $"Marketplace";
						value = Instance.Config.Features.EnableMarketplace.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminFlipbook:
						name = $"Flipbook";
						value = Instance.Config.Features.EnableFlipbook.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminLocation:
						name = $"Location";
						value = Instance.Config.Features.EnableLocation.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminAddPhoto:
						name = $"Add Photo";
						value = Instance.Config.Features.EnableAddPhoto.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminAddCassette:
						name = $"Add Cassette";
						value = Instance.Config.Features.EnableAddCassette.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminRecordMemo:
						name = $"Record Memo";
						value = Instance.Config.Features.EnableRecordMemo.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminAddGif:
						name = $"Add GIFs (Verified Only)";
						value = Instance.Config.Features.EnableAddGifPhoto.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminAutoHolidayMode:
						name = $"Auto-Holiday Mode";
						value = Instance.Config.Features.AutoHolidayMode.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminSimultaneousStoriesPosts:
						name = $"Simultaneous Posts";
						value = Instance.Config.Stories.MaximumSimultaneousPosts.ToString();
						option = OptionTypes.Input;
						break;

					case SettingTypes.AdminPlayStartupSound:
						name = $"Play Startup";
						value = Instance.Config.Sounds.PlayStartup.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminPlayBeepSound:
						name = $"Play Beep";
						value = Instance.Config.Sounds.PlayBeeps.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminPlayLikesSound:
						name = $"Play Like";
						value = Instance.Config.Sounds.PlayLikes.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminPlayDislikeSound:
						name = $"Play Dislike";
						value = Instance.Config.Sounds.PlayDislikes.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminPlayVibrations:
						name = $"Play Vibrations";
						value = Instance.Config.Sounds.PlayVibrations.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminMustBeFriendsToDM:
						name = $"Must Be Friends To DM";
						value = Instance.Config.DMs.MustBeFriendsToDM.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminAutoTeamGroups:
						name = $"Auto Team Groups";
						value = Instance.Config.DMs.AutoTeamGroups.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminDisableBlur:
						name = $"Disable Blur";
						value = Instance.Config.Look.DisableBlur.ToString();
						option = OptionTypes.Button;
						break;

					case SettingTypes.AdminBackgroundOpacity:
						name = $"Background Opacity";
						value = Instance.Config.Look.BackgroundOpacity.ToString();
						option = OptionTypes.Input;
						break;

					case SettingTypes.AdminAdminNameColor:
						name = $"Admin Name";
						value = Instance.Config.Look.AdminNameColor;
						option = OptionTypes.Color;
						break;

					case SettingTypes.AdminModeratorNameColor:
						name = $"Moderator Name";
						value = Instance.Config.Look.ModeratorNameColor;
						option = OptionTypes.Color;
						break;

					case SettingTypes.AdminDeveloperNameColor:
						name = $"Developer Name";
						value = Instance.Config.Look.DeveloperNameColor;
						option = OptionTypes.Color;
						break;

					case SettingTypes.AdminAdvert1wCoupon:
						{
							var coupon = Instance.Config.Advert.Advert1wCoupon;
							name = $"Advert (1w) Coupon";
							value = coupon == null ? "None" : $"{coupon.Code} (<color=green>-{coupon.Discount}</color>)";
							option = OptionTypes.Button;
							break;
						}
					case SettingTypes.AdminAdvert24hCoupon:
						{
							var coupon = Instance.Config.Advert.Advert24hCoupon;
							name = $"Advert (24h) Coupon";
							value = coupon == null ? "None" : $"{coupon.Code} (<color=green>-{coupon.Discount}</color>)";
							option = OptionTypes.Button;
							break;
						}
					case SettingTypes.AdminAdvertShortFlipbookCoupon:
						{
							var coupon = Instance.Config.Advert.AdvertShortFlipbookCoupon;
							name = $"Short Flipbook — Advert Coupon";
							value = coupon == null ? "None" : $"{coupon.Code} (<color=green>-{coupon.Discount}</color>)";
							option = OptionTypes.Button;
							break;
						}
					case SettingTypes.AdminAdvertMediumFlipbookCoupon:
						{
							var coupon = Instance.Config.Advert.AdvertMediumFlipbookCoupon;
							name = $"Medium Flipbook — Advert Coupon";
							value = coupon == null ? "None" : $"{coupon.Code} (<color=green>-{coupon.Discount}</color>)";
							option = OptionTypes.Button;
							break;
						}
					case SettingTypes.AdminAdvertLongFlipbookCoupon:
						{
							var coupon = Instance.Config.Advert.AdvertLongFlipbookCoupon;
							name = $"Long Flipbook — Advert Coupon";
							value = coupon == null ? "None" : $"{coupon.Code} (<color=green>-{coupon.Discount}</color>)";
							option = OptionTypes.Button;
							break;
						}

					case SettingTypes.AdminAdvert24hPrice:
						{
							var price = Instance.Config.Ads.AdvertPrice24h;
							name = $"Advert (24w) Price";
							value = $"{Instance.Config.Currency.GetValueName(this, price)}";
							option = OptionTypes.Input;
							break;
						}
					case SettingTypes.AdminAdvertShortFlipbookPrice:
						{
							var price = Instance.Config.Ads.AdvertShortFlipbookPrice;
							name = $"Short Flipbook — Advert Price";
							value = $"{Instance.Config.Currency.GetValueName(this, price)}";
							option = OptionTypes.Input;
							break;
						}
					case SettingTypes.AdminAdvertMediumFlipbookPrice:
						{
							var price = Instance.Config.Ads.AdvertMediumFlipbookPrice;
							name = $"Medium Flipbook — Advert Price";
							value = $"{Instance.Config.Currency.GetValueName(this, price)}";
							option = OptionTypes.Input;
							break;
						}
					case SettingTypes.AdminAdvertLongFlipbookPrice:
						{
							var price = Instance.Config.Ads.AdvertLongFlipbookPrice;
							name = $"Long Flipbook — Advert Price";
							value = $"{Instance.Config.Currency.GetValueName(this, price)}";
							option = OptionTypes.Input;
							break;
						}
					case SettingTypes.AdminFlipbookResetPrice:
						{
							var price = Instance.Config.Ads.FlipbookResetPrice;
							name = $"Flipbook Reset — Price";
							value = $"{Instance.Config.Currency.GetValueName(this, price)}";
							option = OptionTypes.Input;
							break;
						}
					case SettingTypes.AdminTradePrice:
						{
							var price = Instance.Config.Trade.TradingPrice;
							name = $"Trading — Price";
							value = $"{Instance.Config.Currency.GetValueName(this, price)}";
							option = OptionTypes.Input;
							break;
						}

					case SettingTypes.AdminStockValue:
						name = $"Stock Value";
						value = $"{Instance.Config.Currency.GetValueName(this, Instance.Data.Stock.Value, useBoldValue: true)}";
						option = OptionTypes.Input;
						break;
					case SettingTypes.AdminStartLotteryEvent:
						name = $"Lottery Event";
						value = Instance.LotteryEvent == null ? "Start" : "Stop";
						option = OptionTypes.Button;
						break;
					case SettingTypes.AdminLotteryEventDuration:
						name = $"   Event Duration";
						value = $"{Humanlights.Extensions.TimeEx.Format(Instance.Config.Lottery.EventDuration, false)}";
						option = OptionTypes.Input;
						break;
					case SettingTypes.AdminLotteryEventMinimumTax:
						name = $"   Minimum Tax";
						value = $"{Instance.Config.Currency.GetValueName(this, Instance.Config.Lottery.TaxThreshold, useBoldValue: true)}";
						option = OptionTypes.Input;
						break;
					case SettingTypes.AdminLotteryEventMinimumTickets:
						name = $"   Minimum Tickets";
						value = $"{Instance.Config.Lottery.MinimumTickets:n0}";
						option = OptionTypes.Input;
						break;
					case SettingTypes.AdminLotteryEventMinimumPlayers:
						name = $"   Minimum Players";
						value = $"{Instance.Config.Lottery.MinimumPlayers:n0}";
						option = OptionTypes.Input;
						break;
					case SettingTypes.AdminImgurClientId:
						name = $"Imgur Client ID";
						value = $"Modify";
						option = OptionTypes.Button;
						break;
				}

				#endregion

				#region Customization

				switch (setting)
				{
					case SettingTypes.FeedBackgroundColor:
						name = $"Feed Background Color";
						value = Instance.Data.GetFeed(User).HexBackgroundColor;
						option = OptionTypes.Color;
						break;
					case SettingTypes.FeedTitleColor:
						name = $"Feed Title Color";
						value = Instance.Data.GetFeed(User).HexTitleColor;
						option = OptionTypes.Color;
						break;

					case SettingTypes.CommunityBackgroundColor:
						name = $"Community Background Color";
						value = Instance.Data.GetCommunityFeed().HexBackgroundColor;
						option = OptionTypes.Color;
						break;
					case SettingTypes.CommunityTitleColor:
						name = $"Community Title Color";
						value = Instance.Data.GetCommunityFeed().HexTitleColor;
						option = OptionTypes.Color;
						break;

					case SettingTypes.MarketplaceBackgroundColor:
						name = $"Marketplace Background Color";
						value = Instance.Data.GetMarketplaceFeed().HexBackgroundColor;
						option = OptionTypes.Color;
						break;
					case SettingTypes.MarketplaceTitleColor:
						name = $"Marketplace Title Color";
						value = Instance.Data.GetMarketplaceFeed().HexTitleColor;
						option = OptionTypes.Color;
						break;
				}

				#endregion

				#region System

				switch (setting)
				{
					case SettingTypes.Version:
						name = $"<b>{(Instance.IsRunningLatestVersion() ? "Lastest Version" : $"<color=yellow>Update Available</color> — Get latest on Codefling.com")}</b> — v{Instance.LatestVersion}";
						option = OptionTypes.Blank;
						break;

					case SettingTypes.UpdateLanguages:
						name = "<b>Update Languages</b><size=8>\nThis will download & install all original language and phrases of Ruster.NET.</size>";
						value = "Update";
						option = OptionTypes.Button;
						break;

					case SettingTypes.Save:
						name = string.Empty;
						value = "Save";
						option = OptionTypes.Button;
						break;

					case SettingTypes.Reload:
						name = $"WARNING. This will forcefully reload the plugin.";
						value = "Reload";
						option = OptionTypes.Button;
						break;
				}

				#endregion

				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0.75", }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"0 {yOffset}", OffsetMax = $"0 {yOffset}" }, CursorEnabled = true }, parent);

				container.Add(new CuiLabel { Text = { Text = $"{name}{(option == OptionTypes.Input ? $" ({value})" : "")}", FontSize = 12, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 1" } }, panel);

				switch (option)
				{
					case OptionTypes.Button:
						var boolean = value.ToBool();
						var showValue = false;
						if (!bool.TryParse(value, out boolean)) { boolean = true; showValue = true; }
						container.Add(new CuiButton { Button = { Command = $"{UpdateUserSettingsCmd} {User.Id} {(int)setting} {value}", Color = boolean ? CloseButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = "0.85 0.2", AnchorMax = $"0.985 0.8" }, Text = { Text = showValue ? value : boolean ? "Enabled" : "Disabled", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = boolean ? "0 0 0 1" : "1 1 1 0.3" } }, panel);
						break;

					case OptionTypes.Color:
						var color = "1 1 1 0.6";
						var rawColor = Color.white;
						if (ColorUtility.TryParseHtmlString(value, out rawColor))
						{
							color = $"{rawColor.r} {rawColor.g} {rawColor.b} 0.6";
						}

						container.Add(new CuiButton { Button = { Command = $"{UpdateUserSettingsCmd} {User.Id} {(int)setting} {value}", Color = color }, RectTransform = { AnchorMin = "0.85 0.2", AnchorMax = $"0.985 0.8" }, Text = { Text = String.Empty, FontSize = 9, Align = TextAnchor.MiddleCenter } }, panel);
						break;

					case OptionTypes.Input:
						var input = container.Add(new CuiPanel { Image = { Color = "1 1 1 0.15", }, RectTransform = { AnchorMin = "0.85 0.2", AnchorMax = $"0.985 0.8", OffsetMin = $"0 {yOffset}", OffsetMax = $"0 {yOffset}" }, CursorEnabled = true }, parent);
						container.Add(new CuiElement
						{
							Parent = input,
							Components =
							{
								new CuiInputFieldComponent { Text = "", FontSize = 10, Command = $"{UpdateUserSettingsCmd} {User.Id} {( int )setting}", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", CharsLimit = 16 },
								new CuiRectTransformComponent { AnchorMin = "0.05 0", AnchorMax = $"1 1"}
							}
						});
						break;

					case OptionTypes.Enum:
						container.Add(new CuiButton { Button = { Command = $"", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0.85 0.2", AnchorMax = $"0.985 0.8" }, Text = { Text = value, FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, panel);
						container.Add(new CuiButton { Button = { Command = $"{UpdateUserSettingsCmd} {User.Id} {(int)setting} 0", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.85 0.2", AnchorMax = $"0.875 0.8" }, Text = { Text = "<", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, panel);
						container.Add(new CuiButton { Button = { Command = $"{UpdateUserSettingsCmd} {User.Id} {(int)setting} 1", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.96 0.2", AnchorMax = $"0.985 0.8" }, Text = { Text = ">", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = DefaultFont, Color = "0 0 0 1" } }, panel);
						break;
				}
			}
			public void DrawUserSettingCategory(string title, string parent, float yOffset, CuiElementContainer container)
			{
				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"0 {yOffset}", OffsetMax = $"0 {yOffset}" }, CursorEnabled = true }, parent);

				container.Add(new CuiLabel { Text = { Text = $"<b>{title}</b>", FontSize = 16, Color = "1 1 1 0.3", Font = DefaultFont, Align = TextAnchor.LowerLeft }, RectTransform = { AnchorMin = $"0.025 0.025", AnchorMax = $"1 1" } }, panel);
			}
			public void DrawUserSettingPaging(Page page, string parent, float yOffset, CuiElementContainer container)
			{
				var panel = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"0 {yOffset}", OffsetMax = $"0 {yOffset}" }, CursorEnabled = true }, parent);

				if (page.TotalPages != -1)
				{
					var background = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.01 0.15", AnchorMax = $"0.45 0.85" }, CursorEnabled = true }, panel);
					container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
					container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.65 0", AnchorMax = $"1 1" } }, background);

					var inputPanel = container.Add(new CuiPanel { Image = { Color = DeselectedButtonColor, }, RectTransform = { AnchorMin = $"0.5015 0", AnchorMax = $"0.61 1" }, CursorEnabled = true }, background);
					container.Add(new CuiElement
					{
						Parent = inputPanel,
						Components =
							{
								new CuiInputFieldComponent { Text = "", FontSize = 10, Font = DefaultFont, Command = $"{SetPageCmd} {User.Id} {page.Id} ", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", CharsLimit = 4 },
								new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
							}
					});
				}
			}

			public void DrawNotificationTray()
			{
				var anyUnread = User.Notifications.Any(x => !x.IsRead);
				var container = Container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(User.Configuration.PinNotificationTray ? 0.5 : 0)}", }, RectTransform = { AnchorMin = $"0.8075 0.97", AnchorMax = $"0.99 1" }, CursorEnabled = true }, Background);
				Container.Add(new CuiLabel { Text = { Text = $"<b>{GetPhrase("notifications")}</b> {User.Notifications.Count(x => !x.IsRead):n0}", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleLeft, Color = "1 1 1 0.6" }, RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1" } }, container);
				Container.Add(new CuiButton { Button = { Command = $"{PinNotificationTrayToggleCmd} {User.Id}", Color = User.Configuration.PinNotificationTray ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"0.875 0.15", AnchorMax = $"0.97 0.85" }, Text = { Align = TextAnchor.MiddleCenter, Text = User.Configuration.PinNotificationTray ? GetPhrase("hide") : GetPhrase("show"), FontSize = 8, Color = User.Configuration.PinNotificationTray ? "0 0 0 1" : "1 1 1 1" } }, container);

				if (User.Configuration.PinNotificationTray)
				{
					Container.Add(new CuiButton { Button = { Command = $"{ReadAllNotificationsCmd} {User.Id}", Color = anyUnread ? NormalButtonColor : ArrowButtonColor }, RectTransform = { AnchorMin = $"0.625 0.15", AnchorMax = $"0.86 0.85" }, Text = { Align = TextAnchor.MiddleCenter, Text = GetPhrase("readall"), FontSize = 8, Color = anyUnread ? "0 0 0 1" : "1 1 1 1" } }, container);

					container = Container.Add(new CuiPanel { Image = { Color = "0 0 0 0.5", }, RectTransform = { AnchorMin = $"0.8075 0.75", AnchorMax = $"0.99 0.9675" }, CursorEnabled = true }, Background);

					var page = GetPage(80);
					var anchorMin = 0.815f;
					var anchorMax = 0.95f;
					var spacing = 0.16f;
					var notifications = User.Notifications;
					page.TotalPages = (int)Math.Ceiling((double)notifications.Count / NotificationsPerPage - 1);

					var pageNotifications = notifications.Skip(NotificationsPerPage * page.CurrentPage).Take(NotificationsPerPage).ToArray();
					page.Check();

					foreach (var notification in pageNotifications)
					{
						var n = Container.Add(new CuiPanel { Image = { Color = $"0 0 0 {(notification.IsRead ? 0.25 : 0.75)}", }, RectTransform = { AnchorMin = $"0.01 {anchorMin}", AnchorMax = $"0.99 {anchorMax}" }, CursorEnabled = true }, container);
						Container.Add(new CuiLabel { Text = { Text = $"{notification.Content}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.LowerLeft }, RectTransform = { AnchorMin = $"0.02 0.1", AnchorMax = $"0.98 1" } }, n);
						Container.Add(new CuiLabel { Text = { Text = $"[ <size=8><b>{notification.NotificationType.ToString().ToUpper()}</b></size> ] — {User.GetTimeSpan(notification.Ticks, User.Id, true)}", FontSize = 7, Color = "0.9 0.4 0.3 0.5", Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.02 0.15", AnchorMax = $"0.98 0.875" } }, n);

						Container.Add(new CuiButton { Button = { Command = $"{DeleteNotificationCmd} {User.Id} {notification.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = $"{(notification.IsRead ? 0.715 : 0.73)} 0.15", AnchorMax = $"{(notification.IsRead ? 0.835 : 0.86)} 0.85" }, Text = { Align = TextAnchor.MiddleCenter, Text = GetPhrase("delete"), FontSize = 7, Color = "1 1 1 1" } }, n);
						Container.Add(new CuiButton { Button = { Command = $"{ReadNotificationCmd} {User.Id} {notification.Id}", Color = notification.IsRead ? ArrowButtonColor : CloseButtonColor }, RectTransform = { AnchorMin = $"{(notification.IsRead ? 0.85 : 0.875)} 0.15", AnchorMax = $"0.97 0.85" }, Text = { Align = TextAnchor.MiddleCenter, Text = notification.IsRead ? GetPhrase("unread") : GetPhrase("read"), FontSize = 7, Color = notification.IsRead ? "1 1 1 1" : "0 0 0 1" } }, n);

						anchorMin -= spacing;
						anchorMax -= spacing;
					}

					if (notifications.Count == 0)
					{
						Container.Add(new CuiLabel { Text = { Text = GetPhrase("nonotifications"), FontSize = 9, Color = "1 1 1 0.4", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0.125", AnchorMax = $"1 1" } }, container);
					}
					else
					{
						var b = Container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.05 0.01", AnchorMax = $"0.75 0.125" }, CursorEnabled = true }, container);
						Container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
						Container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
						Container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
						Container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = DeselectedButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, b);
						Container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, b);
					}
				}
			}

			private void DrawUsers(string title, RusterUser[] users, int perPage, string noUsersContent, CuiElementContainer container, string parent, int pageId, float anchorMin = 0.79f, float anchorMax = 0.915f, float spacing = 0.115f, string command = null)
			{
				var backgroundColor = "0.2 0.2 0.2 0.5";
				var background = container.Add(new CuiPanel { Image = { Color = "0 0 0 0", }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, parent);
				var usersParent = container.Add(new CuiPanel { Image = { Color = backgroundColor, }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, background);
				{
					var page = GetPage(pageId);

					var pageUsers = users.Skip(perPage * page.CurrentPage).Take(perPage).ToArray();
					page.TotalPages = (int)Math.Ceiling((double)users.Length / perPage - 1);
					page.Check();

					container.Add(new CuiLabel { Text = { Text = $"<b>{title}</b> {users.Length:n0}", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.05 0.025", AnchorMax = $"0.975 0.975" } }, usersParent);

					foreach (var friend in pageUsers)
					{
						DrawListUser(friend, false, false, container, usersParent, anchorMin, anchorMax, interactible: ServerViewer.CurrentServer == null, command: command);
						anchorMin -= spacing;
						anchorMax -= spacing;
					}
					if (page.TotalPages > -1)
					{
						var buttons = container.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0", }, RectTransform = { AnchorMin = $"0.025 0.01", AnchorMax = $"0.85 0.05" }, CursorEnabled = true }, usersParent);

						container.Add(new CuiButton { Button = { Command = $"{StartPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.1 1" }, Text = { Text = $"◀◀", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiButton { Button = { Command = $"{PrevPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.125 0", AnchorMax = $"0.225 1" }, Text = { Text = $"◀", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiButton { Button = { Command = $"{NextPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.25 0", AnchorMax = $"0.35 1" }, Text = { Text = $"▶", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiButton { Button = { Command = $"{EndPageCmd} {User.Id} {page.Id}", Color = ArrowButtonColor }, RectTransform = { AnchorMin = "0.375 0", AnchorMax = $"0.475 1" }, Text = { Text = $"▶▶", FontSize = 7, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, buttons);
						container.Add(new CuiLabel { Text = { Text = $"{page.CurrentPage + 1:n0} / {page.TotalPages + 1:n0}", FontSize = 8, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.525 0", AnchorMax = $"1 1" } }, buttons);
					}
					else
					{
						container.Add(new CuiLabel { Text = { Text = noUsersContent, FontSize = 10, Color = "1 1 1 0.5", Font = DefaultFont, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" } }, usersParent);
					}
				}
			}

			public void Notify(string title, string content, float duration = 7.5f, bool isBackground = false, bool playSound = true, Action<RusterBrowser> onRead = null, string openName = "")
			{
				if (Player == null || !User.IsOnline() || !User.Configuration.PushNotifications) return;

				var extraTime = MainFadeout * 1.5f;
				if (!Player.IsConnected) return;

				CloseNotice();

				if (User.Configuration.PerformanceMode) isBackground = true;

				if (!isBackground) Notify(title, content, duration, isBackground: true, playSound: playSound, onRead: onRead, openName: openName);

				var container = new CuiElementContainer();
				var mainBackground = container.Add(new CuiPanel { Image = { Color = $"0.1 0.1 0.1 {(Instance.Config.Look.DisableBlur ? 0 : 0.3)}", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0.8 0.85", AnchorMax = "0.99 0.9775", OffsetMin = $"0 {Instance.Config.Notifications.VerticalOffset}", OffsetMax = $"0 {Instance.Config.Notifications.VerticalOffset}" }, CursorEnabled = false }, "Overlay", !isBackground ? NoticeCUI : NoticeGhostCUI);
				var background = container.Add(new CuiPanel { Image = { Color = CloseButtonColor, }, RectTransform = { AnchorMin = "0 0", AnchorMax = "0.05 1" }, CursorEnabled = false }, mainBackground);

				container.Add(new CuiButton { Button = { Command = $"{CloseNoticeCmd} {User.Id}", Color = CloseButtonColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1" }, Text = { Text = "", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } }, background);
				container.Add(new CuiLabel { Text = { Text = $"<b>{title.Replace("\\\"", "\"").ToUpper()}</b>", FontSize = 15, Font = DefaultFont, Align = TextAnchor.UpperLeft, Color = CloseButtonColor }, RectTransform = { AnchorMin = $"0.1 0", AnchorMax = $"0.9 0.85" } }, mainBackground);
				container.Add(new CuiLabel { Text = { Text = $"{content.Replace("\\\"", "\"")}", FontSize = 10, Font = DefaultFont, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = $"0.1 0", AnchorMax = $"0.9 0.65" } }, mainBackground);
				container.Add(new CuiButton { Button = { Command = $"{ReadNoticeCmd} {User.Id}", Color = onRead == null ? ArrowButtonColor : CloseButtonColor }, RectTransform = { AnchorMin = "0.1 0.1", AnchorMax = $"0.95 0.3" }, Text = { Text = $"Open <b>{(string.IsNullOrEmpty(openName) ? Instance.Name.ToUpper() : openName)}</b>", FontSize = 10, Font = DefaultFont, Align = TextAnchor.MiddleCenter, Color = onRead == null ? "1 1 1 0.5" : "1 1 1 1" } }, mainBackground);

				OnNotificationRead = onRead;

				ServerMgr.Instance.Invoke(() =>
				{
					CloseNotice();
					CuiHelper.AddUi(Player, container);
				}, extraTime);

				if (CurrentAction != null) Player.CancelInvoke(CurrentAction);
				CurrentAction = new Action(() => { CloseNotice(); });

				Player.Invoke(CurrentAction, duration + extraTime);

				if (isBackground)
				{
					PlayVibration();
				}
			}
			public void NotifyRustPlus(string title, string content, bool forceSend = false)
			{
				if ((string.IsNullOrEmpty(title) && !forceSend) || User.IsOnline() || IsRustPlusNotificationCooldown() || !User.Configuration.RustPlusNotifications) return;

				try
				{
					NotificationList.SendNotificationTo(User.Id,
								NotificationChannel.SmartAlarm,
								$"{title} | Ruster.NET on {ConVar.Server.hostname}",
								$"{content}",
								new Dictionary<string, string>());
				}
				catch { }
			}
			public void NotifyChat(string title, string content, bool playSound = true)
			{
				if (Player == null || !User.Configuration.ChatNotifications || IsOpen) return;

				Instance.Print($"{title}<size=10>\n{content}</size>", Player, true);

				if (playSound) PlayVibration();
			}

			public static T AppendEnum<T>(int value, bool up)
			{
				var values = Enum.GetNames(typeof(T));
				var count = values.Length;

				if (up)
				{
					return (T)Enum.Parse(typeof(T), values[(value + 1) > count - 1 ? 0 : value + 1]);
				}
				else
				{
					return (T)Enum.Parse(typeof(T), values[(value - 1) < 0 ? values.Length - 1 : value - 1]);
				}
			}

			public void PlayLike()
			{
				if (!Instance.Config.Sounds.PlayLikes) return;

				SendEffectTo(Player, effect: "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab");
			}
			public void PlayDislike()
			{
				if (!Instance.Config.Sounds.PlayDislikes) return;

				SendEffectTo(Player, effect: "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab");
			}
			public void PlayBoop()
			{
				if (!Instance.Config.Sounds.PlayBeeps) return;

				SendEffectTo(Player, effect: "assets/prefabs/locks/keypad/effects/lock.code.lock.prefab");
			}
			public void PlayVibration()
			{
				if (!Instance.Config.Sounds.PlayVibrations) return;

				SendEffectTo(Player, effect: "assets/prefabs/tools/pager/effects/vibrate.prefab");
			}

			public Page GetPage(int id)
			{
				if (!Pages.ContainsKey(id))
				{
					var page = new Page(id);
					Pages.Add(id, page);
					return page;
				}

				return Pages[id];
			}
			public static string CountFormat(int num)
			{
				if (num >= 10000000)
					return $"<b>{num / 1000000:#,0}</b>M";

				if (num >= 1000000)
					return $"<b>{num / 1000000:0.#}</b>M";

				if (num >= 10000)
					return $"<b>{num / 1000:#,0}</b>K";

				if (num >= 1000)
					return $"<b>{num / 1000:0.#}</b>K";

				return num.ToString("#,0");
			}

			#region Player

			public string GetPhrase(string key, params object[] parameters)
			{
				try { return string.Format(Instance.lang.GetMessage(key, Instance, User?.Id.ToString()), parameters); } catch { return key; }
			}
			public string GetPhrase(ItemDefinition item)
			{
				try
				{
					var key = $"item_{item.shortname}";
					var translation = Instance.lang.GetMessage(key, Instance, User?.Id.ToString());
					if (translation == key) translation = item.displayName.english;

					return translation;
				}
				catch { return item.displayName.english; }
			}

			public string GetLanguage()
			{
				return User.Configuration.Language;
			}
			public void SetLanguage(string language)
			{
				var previousLanguage = Instance.lang.GetLanguage(User.Id.ToString());
				if (previousLanguage == language) return;

				try
				{
					Instance.lang.SetLanguage(User.Configuration.Language = language, User.Id.ToString());
					Instance.RusterAddons?.Call("RNETAPI_OnLanguageChange", User.Id, language, previousLanguage);
				}
				catch { }
			}

			public BaseMountable Mountable { get; set; }
			public void LockPlayer()
			{
				if (Mountable != null || Player.isMounted) return;

				var position = Player.transform.position;
				Mountable = GameManager.server.CreateEntity("assets/prefabs/vehicle/seats/standingdriver.prefab", Player.transform.position, Player.transform.rotation, true) as BaseMountable;

				Mountable.transform.localPosition = Vector3.zero;
				Mountable.transform.localEulerAngles = Vector3.zero;
				Mountable.transform.SetPositionAndRotation(position, Player.transform.rotation);

				Mountable.Spawn();
				Mountable.EnableGlobalBroadcast(true);

				Player.EnsureDismounted();
				Player.MountObject(Mountable);
				Mountable.MountPlayer(Player);

				Mountable.transform.position = position;
				Mountable.SendNetworkUpdate();
			}
			public void UnlockPlayer()
			{
				try
				{
					if (Mountable == null && Player.isMounted) return;

					Mountable?.Kill();
					Player?.EnsureDismounted();
					Player?.DismountObject();

					Mountable = null;
				}
				catch { }
			}

			public ItemContainer CreateContainer(string onlyAllowedItemShorname, int maxStackSize = 1, int capacity = 1, Func<Item, int, bool> canAcceptItem = null)
			{
				var container = new ItemContainer
				{
					isServer = true,
					allowedContents = ItemContainer.ContentsType.Generic,
					capacity = capacity,
					maxStackSize = maxStackSize
				};

				if (canAcceptItem != null) container.canAcceptItem = canAcceptItem;

				if (!string.IsNullOrEmpty(onlyAllowedItemShorname))
					container.SetOnlyAllowedItem(ItemManager.FindItemDefinition(onlyAllowedItemShorname));

				container.GiveUID();

				return container;
			}
			public void OpenPhotographPanel(BasePlayer player)
			{
				PhotographContainer.entityOwner = PhotographContainer.playerOwner = player;

				PlayerLootContainer(player, "photoframe", PhotographContainer);
			}
			public void OpenSoldItemPanel(BasePlayer player)
			{
				SellingContainer.entityOwner = SellingContainer.playerOwner = player;

				PlayerLootContainer(player, "genericsmall", SellingContainer);
			}
			public void OpenCassettePanel(BasePlayer player)
			{
				CassetteContainer.entityOwner = CassetteContainer.playerOwner = player;

				PlayerLootContainer(player, "cassettereceiver", CassetteContainer);
			}
			public void OpenGiftBasketPanel(BasePlayer player, RusterUser basketUser = null)
			{
				if (basketUser == null) basketUser = User;

				GiftBasketContainer.entityOwner = GiftBasketContainer.playerOwner = player;
				GiftBasketContainer.SetFlag(ItemContainer.Flag.NoItemInput, false);

				var basket = Instance.Data.GetGiftBasket(basketUser);
				foreach (var item in basket)
				{
					var basketItem = item.CreateItem();
					basketItem.MoveToContainer(GiftBasketContainer, allowStack: false, ignoreStackLimit: true);
				}

				GiftBasketContainer.SetFlag(ItemContainer.Flag.NoItemInput, true);

				PlayerLootContainer(player, "genericsmall", GiftBasketContainer);
			}
			public void OpenTrade(BasePlayer customer)
			{
				Trade?.Kill();

				Trade = GameManager.server.CreateEntity("assets/bundled/prefabs/static/wall.frame.shopfront.metal.static.prefab") as ShopFront;
				Trade.globalBroadcast = true;
				Trade.enableSaving = false;
				Trade.name = "Ruster.NET Trade";

				var customerBrowser = Instance.GetBrowser(customer);
				customerBrowser.Trade = Trade;

				UnityEngine.Object.Destroy(Trade.GetComponent<DestroyOnGroundMissing>());
				UnityEngine.Object.Destroy(Trade.GetComponent<GroundWatch>());
				Trade.Spawn();

				Trade.customerInventory.MarkDirty();
				Trade.vendorInventory.MarkDirty();
				Trade.customerInventory.onItemAddedRemoved += (item, b) => { if (b) item.OnDirty += ResetTrade; else item.OnDirty -= ResetTrade; };
				Trade.vendorInventory.onItemAddedRemoved += (item, b) => { if (b) item.OnDirty += ResetTrade; else item.OnDirty -= ResetTrade; };
				Trade.customerInventory.canAcceptItem += (item, i) => CanAcceptTradeItem(item, i, Trade, false);
				Trade.vendorInventory.canAcceptItem += (item, i) => CanAcceptTradeItem(item, i, Trade, true);

				PlayerLootContainer(customer, "shopfront", Trade.vendorInventory);
				PlayerLootContainer(Player, "shopfront", Trade.vendorInventory);

				Trade.vendorPlayer = customer;
				customer.inventory.loot.AddContainer(Trade.customerInventory);
				customer.inventory.loot.SendImmediate();

				Trade.customerPlayer = Player;
				Player.inventory.loot.AddContainer(Trade.customerInventory);
				Player.inventory.loot.SendImmediate();

				Trade.UpdatePlayers();
			}
			private bool CanAcceptTradeItem(Item item, int slot, ShopFront entity, bool forVendor)
			{
				var itemOwner = item.GetOwnerPlayer();
				var itemParent = item.parent;
				var allowedPlayer = forVendor ? entity.vendorPlayer : entity.customerPlayer;
				var allowedInventory = forVendor ? entity.vendorInventory : entity.customerInventory;
				var pass1 = allowedPlayer == itemOwner;
				var pass2 = itemParent == allowedInventory;
				var pass3 = allowedInventory.GetSlot(slot) == null;

				if ((pass1 || pass2) && pass3)
				{
					RemoveTradeMods(item);
					return true;
				}
				else return false;
			}
			private void RemoveTradeMods(Item item)
			{
				if (item == null) return;

				var parent = item.parent;

				if (parent != null && item.contents != null)
				{
					var items = Facepunch.Pool.GetList<Item>();
					items.AddRange(item.contents.itemList);

					foreach (var mod in items)
					{
						if (mod.MoveToContainer(parent) == false)
						{
							mod.Drop(item.parent.dropPosition, Vector3.zero);
						}
					}

					Facepunch.Pool.FreeList(ref items);
				}
			}
			private void ResetTrade(Item item)
			{
				var parent = item.parent?.entityOwner as ShopFront;

				if (parent != null)
				{
					parent.ResetTrade();
				}
			}

			private void PlayerLootContainer(BasePlayer player, string lootPanel, params ItemContainer[] containers)
			{
				player.inventory.loot.Clear();
				player.inventory.loot.PositionChecks = false;
				player.inventory.loot.entitySource = containers[0].entityOwner ?? player;
				player.inventory.loot.itemSource = null;
				player.inventory.loot.MarkDirty();
				foreach (var container in containers) player.inventory.loot.AddContainer(container);
				player.inventory.loot.SendImmediate();

				player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", lootPanel);
				player.SendNetworkUpdateImmediate();
			}
			public MonumentInfo GetNearbyMonument()
			{
				return Instance.Monuments.OrderBy(x => Vector3.Distance(x.transform.position, Player.transform.position)).FirstOrDefault();
			}
			public string GetGrid(Vector3 position)
			{
				return (Instance.GridAPI.CallHook("GetGrid", position) as string[]).ToString("", "");
			}

			public bool PlayerHasCurrency(int amount)
			{
				return GetPlayerCurrency() >= amount;
			}
			public bool TakePlayerCurrency(int amount)
			{
				if (!PlayerHasCurrency(amount)) return false;

				switch (User.Configuration.PaymentMethod)
				{
					case RusterUserConfiguration.PaymentMethods.Currency:
						switch (Instance.Config.Currency.CurrencyType)
						{
							case RootConfig.CurrencyConfig.CurrencyTypes.ServerRewards:
								return (bool)Instance.ServerRewards.Call("TakePoints", Player.userID, amount);

							case RootConfig.CurrencyConfig.CurrencyTypes.Economics:
								return (bool)Instance.Economics.Call("Withdraw", Player.userID, (double)amount);

							case RootConfig.CurrencyConfig.CurrencyTypes.Other:
								switch (Instance.Config.Currency.OtherSettings.TypeMode)
								{
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Int: return (bool)Instance.OtherPlugin.Call(Instance.Config.Currency.OtherSettings.WithdrawMethod, Player.userID, amount);
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Double: return (bool)Instance.OtherPlugin.Call(Instance.Config.Currency.OtherSettings.WithdrawMethod, Player.userID, (double)amount);
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Float: return (bool)Instance.OtherPlugin.Call(Instance.Config.Currency.OtherSettings.WithdrawMethod, Player.userID, (float)amount);
								}
								return false;

							default:
								var amountLeft = amount;
								var currencyItems = Pool.GetList<Item>();
								currencyItems.AddRange(Player.inventory.containerMain.itemList.Where(x => x.info.shortname == Instance.Config.Currency.ItemShortname && (Instance.Config.Currency.ItemSkinId == 0 ? true : x.skin == Instance.Config.Currency.ItemSkinId)));
								currencyItems.AddRange(Player.inventory.containerBelt.itemList.Where(x => x.info.shortname == Instance.Config.Currency.ItemShortname && (Instance.Config.Currency.ItemSkinId == 0 ? true : x.skin == Instance.Config.Currency.ItemSkinId)));

								foreach (var currencyItem in currencyItems)
								{
									if (amountLeft <= 0) break;

									var oldMoneyAmount = currencyItem.amount;

									if (amountLeft >= currencyItem.amount) currencyItem.Remove();
									else currencyItem.amount -= amountLeft;

									amountLeft -= oldMoneyAmount;
								}

								Player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, Player.inventory.containerMain, false);
								Player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, Player.inventory.containerBelt, false);
								ItemManager.DoRemoves();
								Pool.FreeList(ref currencyItems);
								return true;
						}

					case RusterUserConfiguration.PaymentMethods.Wallet:
						User.Wallet -= amount;
						return true;
				}

				return false;
			}
			public bool GivePlayerCurrency(int amount, RusterUserConfiguration.PaymentMethods? overrideUserMethod = null)
			{
				if (overrideUserMethod == null) overrideUserMethod = User.Configuration.PaymentMethod;

				switch (overrideUserMethod)
				{
					case RusterUserConfiguration.PaymentMethods.Currency:
						switch (Instance.Config.Currency.CurrencyType)
						{
							case RootConfig.CurrencyConfig.CurrencyTypes.ServerRewards:
								return (bool)Instance.ServerRewards.Call("AddPoints", Player.userID, amount);

							case RootConfig.CurrencyConfig.CurrencyTypes.Economics:
								return (bool)Instance.Economics.Call("Deposit", Player.userID, (double)amount);

							case RootConfig.CurrencyConfig.CurrencyTypes.Other:
								switch (Instance.Config.Currency.OtherSettings.TypeMode)
								{
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Int: return (bool)Instance.OtherPlugin.Call(Instance.Config.Currency.OtherSettings.DepositMethod, Player.userID, amount);
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Double: return (bool)Instance.OtherPlugin.Call(Instance.Config.Currency.OtherSettings.DepositMethod, Player.userID, (double)amount);
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Float: return (bool)Instance.OtherPlugin.Call(Instance.Config.Currency.OtherSettings.DepositMethod, Player.userID, (float)amount);
								}
								return false;

							default:
								Player.GiveItem(ItemManager.CreateByName(Instance.Config.Currency.ItemShortname, amount, Instance.Config.Currency.ItemSkinId));
								Player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, Player.inventory.containerMain, false);
								Player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, Player.inventory.containerBelt, false);
								return true;
						}

					case RusterUserConfiguration.PaymentMethods.Wallet:
						User.Wallet += amount;
						return true;
				}

				return false;
			}
			public double GetPlayerCurrency()
			{
				switch (User.Configuration.PaymentMethod)
				{
					case RusterUserConfiguration.PaymentMethods.Currency:
						switch (Instance.Config.Currency.CurrencyType)
						{
							case RootConfig.CurrencyConfig.CurrencyTypes.ServerRewards:
								return (int)Instance.ServerRewards?.Call("CheckPoints", Player.userID);

							case RootConfig.CurrencyConfig.CurrencyTypes.Economics:
								return (double)Instance.Economics?.Call("Balance", Player.userID);

							case RootConfig.CurrencyConfig.CurrencyTypes.Other:
								switch (Instance.Config.Currency.OtherSettings.TypeMode)
								{
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Int: return (int)Instance.OtherPlugin?.Call(Instance.Config.Currency.OtherSettings.BalanceMethod, Player.userID);
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Double: return (double)Instance.OtherPlugin?.Call(Instance.Config.Currency.OtherSettings.BalanceMethod, Player.userID);
									case RootConfig.CurrencyConfig.PluginSettings.TypeModes.Float: return (float)Instance.OtherPlugin?.Call(Instance.Config.Currency.OtherSettings.BalanceMethod, Player.userID);
								}
								return 0;

							default:
								var currencyItems = Pool.GetList<Item>();
								currencyItems.AddRange(Player.inventory.containerMain.itemList.Where(x => x.info.shortname == Instance.Config.Currency.ItemShortname && x.skin == Instance.Config.Currency.ItemSkinId));
								currencyItems.AddRange(Player.inventory.containerBelt.itemList.Where(x => x.info.shortname == Instance.Config.Currency.ItemShortname && x.skin == Instance.Config.Currency.ItemSkinId));
								var sum = currencyItems.Sum(x => x.amount);
								Pool.FreeList(ref currencyItems);
								return sum;
						}

					case RusterUserConfiguration.PaymentMethods.Wallet:
						return User.Wallet;
				}

				return 0;
			}

			public bool StorageHasItems(string shortName, int amount, ItemContainer container, ulong? skin = null, bool mustBeMaximumCondition = false)
			{
				var items = Facepunch.Pool.GetList<Item>();
				items.AddRange(container.itemList.Where(x => x.info.shortname == shortName && (skin == null ? true : x.skin == skin.Value) && (!mustBeMaximumCondition ? true : x.condition == x.maxCondition)));
				var currentAmount = items.Sum(x => x.amount);

				Facepunch.Pool.FreeList(ref items);
				return currentAmount >= amount;
			}
			public bool TakeStorageItems(string shortName, int amount, ItemContainer container, ulong? skin = null, bool mustBeMaximumCondition = false)
			{
				if (!StorageHasItems(shortName, amount, container, skin)) return false;

				var items = Facepunch.Pool.GetList<Item>();
				items.AddRange(container.itemList.Where(x => x.info.shortname == shortName && (skin == null ? true : x.skin == skin.Value) && (!mustBeMaximumCondition ? true : x.condition == x.maxCondition)));

				var amountLeft = amount;
				foreach (var item in items)
				{
					if (amountLeft <= 0) break;

					var oldItemAmount = item.amount;

					if (amountLeft >= item.amount) item.Remove();
					else item.amount -= amountLeft;

					amountLeft -= oldItemAmount;
				}

				Facepunch.Pool.FreeList(ref items);
				return true;
			}

			#endregion

			public bool DisableCooldown { get; set; } = false;
			public bool IsButtonCooldown()
			{
				if (DisableCooldown) return false;
				if (ButtonPressCooldown <= Instance.Config.Cooldown.ButtonPress || IsCooledDown) return true;

				ButtonPressCooldown = 0;
				return false;
			}
			public bool IsBusinessCardCooldown(bool resetCooldown = true)
			{
				if (DisableCooldown) return false;
				if (BusinessCardCooldown <= Instance.Config.Cooldown.BusinessCardCreation) return true;

				if (resetCooldown) BusinessCardCooldown = 0;
				return false;
			}
			public bool IsRustPlusNotificationCooldown()
			{
				if (DisableCooldown) return false;
				if (RustPlusNotificationCooldown <= Instance.Config.Cooldown.RustPlusNotifications) return true;

				RustPlusNotificationCooldown = 0;
				return false;
			}
		}

		public class RusterAudioPlayer
		{
			public RusterBrowser Browser { get; }
			public DeployedRecorder Recorder { get; set; }
			public RusterFeed.RusterPost PlayedPost { get; set; }

			public Timer RecorderUpdater { get; set; }

			public RusterAudioPlayer() { }
			public RusterAudioPlayer(RusterBrowser browser) { Browser = browser; }

			public bool IsPlaying() { return Recorder != null; }
			public bool IsPlaying(RusterFeed.RusterPost post) { return Recorder != null && PlayedPost == post; }
			public void Play(RusterFeed.RusterPost post)
			{
				Stop();

				var cassette = BaseNetworkable.serverEntities.Find(new NetworkableId(post.CassetteId)) as Cassette;

				if (cassette != null)
				{
					cassette.SendNetworkUpdate();
					cassette.SendNetworkGroupChange();
					PlayedPost = post;

					var playerPosition = Browser.Player.ServerPosition + (Browser.Player.transform.up * 1.3f) + (Browser.Player.transform.forward * -0.2f);
					Recorder = GameManager.server.CreateEntity("assets/prefabs/voiceaudio/cassetterecorder/cassetterecorder.deployed.prefab", pos: Browser.Player.transform.position, rot: Browser.Player.transform.rotation) as DeployedRecorder;
					Recorder.Spawn();

					Recorder.SetCollisionEnabled(false);
					Recorder.SetMotionEnabled(false);

					Recorder.SendNetworkGroupChange();
					Recorder.SendNetworkUpdate();

					Instance.NextTick(() =>
					{
						Recorder.OnCassetteInserted(cassette);
						Recorder.SetFlag(BaseEntity.Flags.Busy, true);
						Recorder.SetFlag(BaseEntity.Flags.On, true, false, true);
						Recorder.SetFlag(BaseEntity.Flags.On, false, false, true);
						ServerMgr.Instance.Invoke(() => { Recorder.SetFlag(BaseEntity.Flags.On, true, false, true); }, 0.5f);

						Instance.RusterAddons?.Call("RNETAPI_OnPostPlay", Browser.CurrentUserId, post);
					});

					RecorderUpdater = Instance.timer.Every(0.1f, () =>
					{
						if (Browser == null || Recorder == null || Browser.Player == null) return;

						playerPosition = Browser.Player.ServerPosition + (Browser.Player.transform.up * 1.3f);
						Recorder.transform.position = playerPosition;
					});
				}
			}
			public void Play(RusterConversation.RusterDirectMessage message)
			{
				Stop();

				PlayedPost = new RusterFeed.RusterPost
				{
					Id = message.Id,
					UserId = message.SenderId,
					CassetteId = message.CassetteId,
					CassetteTitle = $"Message Memo"
				};

				var cassette = BaseNetworkable.serverEntities.Find(new NetworkableId(message.CassetteId)) as Cassette;

				if (cassette != null)
				{
					cassette.SendNetworkUpdate();
					cassette.SendNetworkGroupChange();

					var playerPosition = Browser.Player.ServerPosition + (Browser.Player.transform.up * 1.3f) + (Browser.Player.transform.forward * -0.2f);
					Recorder = GameManager.server.CreateEntity("assets/prefabs/voiceaudio/cassetterecorder/cassetterecorder.deployed.prefab", pos: Browser.Player.transform.position, rot: Browser.Player.transform.rotation) as DeployedRecorder;
					Recorder.Spawn();

					Recorder.SetCollisionEnabled(false);
					Recorder.SetMotionEnabled(false);

					Recorder.SendNetworkGroupChange();
					Recorder.SendNetworkUpdate();

					Instance.NextTick(() =>
					{
						Recorder.OnCassetteInserted(cassette);
						Recorder.SetFlag(BaseEntity.Flags.Busy, true);
						Recorder.SetFlag(BaseEntity.Flags.On, true, false, true);
						Recorder.SetFlag(BaseEntity.Flags.On, false, false, true);
						ServerMgr.Instance.Invoke(() => { Recorder.SetFlag(BaseEntity.Flags.On, true, false, true); }, 0.5f);
					});

					RecorderUpdater = Instance.timer.Every(0.1f, () =>
					{
						if (Browser == null || Recorder == null || Browser.Player == null) return;

						playerPosition = Browser.Player.ServerPosition + (Browser.Player.transform.up * 1.3f);
						Recorder.transform.position = playerPosition;
					});
				}
			}
			public void Stop(bool clearPost = true)
			{
				if (PlayedPost != null && !PlayedPost.CassetteTitle.Equals("Message Memo"))
				{
					Instance.RusterAddons?.Call("RNETAPI_OnPostStop", Browser.CurrentUserId, PlayedPost);
				}

				RecorderUpdater?.Destroy();
				RecorderUpdater = null;

				Recorder?.Kill();
				Recorder = null;
				if (clearPost) PlayedPost = null;
			}

			public string GetTrackName()
			{
				if (PlayedPost == null) return null;

				return $"{(string.IsNullOrEmpty(PlayedPost.CassetteTitle) ? "" : $"{PlayedPost.CassetteTitle} — ")}{PlayedPost.GetUser().GetDisplayName(false, observer: Browser.User)}".Trim().Truncate(44, "...");
			}
		}
		public class RusterServerViewer
		{
			public global::RusterNET.Core.Server CurrentServer { get; set; }
			public RusterFeed CommunityFeed { get; set; }
			public RusterFeed MarketplaceFeed { get; set; }
			public RusterStory[] Stories { get; set; }
			public RusterUser[] Users { get; set; }

			public bool IsViewing()
			{
				return CurrentServer != null;
			}
			public void Clear()
			{
				CurrentServer = null;
				CommunityFeed = MarketplaceFeed = null;
				Stories = null;
				Users = null;
			}
		}
		public class RusterGIFProcessor
		{
			public RusterBrowser Browser { get; set; }
			public RusterGif CurrentGif { get; set; }
			public Timer FrameRateTimer { get; set; }
			public Timer DurationTimer { get; set; }
			public Action OnRenderStopped { get; set; }

			public bool IsRendering { get; set; }
			public bool IsBackwards { get; set; } = false;
			public int CurrentFrame { get; set; }

			public void InstallGif(RusterGif gif)
			{
				CurrentGif = gif;
				CurrentFrame = 0;
				IsBackwards = false;
			}

			public void StartRender(float frameRate = 0.5f, float duration = 10f)
			{
				StopRender();

				var frames = CurrentGif.FrameUrls;
				Browser.DrawGifPanel();

				FrameRateTimer = Instance.timer.Every(frameRate, () =>
				{
					Browser.DrawGifPanelContent(frames[CurrentFrame], CurrentGif.Bleeding);

					if (IsBackwards) CurrentFrame--; else CurrentFrame++;

					if (CurrentFrame > frames.Count - 1)
					{
						if (CurrentGif.Boomerang)
						{
							IsBackwards = true;
							CurrentFrame--;
						}
						else
						{
							CurrentFrame = 0;
						}
					}
					else if (CurrentFrame < 0)
					{
						if (CurrentGif.Boomerang)
						{
							IsBackwards = false;
							CurrentFrame++;
						}
						else
						{
							CurrentFrame = frames.Count;
						}
					}
				});
				DurationTimer = Instance.timer.In(duration, () => { StopRender(); });

				IsRendering = true;
			}

			public void StopRender()
			{
				DurationTimer?.Destroy();
				DurationTimer = null;
				FrameRateTimer?.Destroy();
				FrameRateTimer = null;

				Browser.CloseBlankContent();
				Browser.CloseBlank();
				IsRendering = false;

				OnRenderStopped?.Invoke();
				OnRenderStopped = null;
			}
		}

		public void NotifyAll(string title, string description, float duration = 7.5f)
		{
			ServerMgr.Instance.Invoke(() => { Notify(Instance.Data.Users.ToArray(), title, description, duration); }, 0.25f);
		}
		public void Notify(RusterUser[] users, string title, string description, float duration = 7.5f)
		{
			foreach (var user in users)
			{
				var player = user.GetPlayer();
				if (player == null) continue;

				var browser = GetBrowser(player);
				browser.Notify(title, description, duration);
			}
		}

		public void Launch(BasePlayer player)
		{
			var browser = GetBrowser(player);
			browser.Player = player;

			if (browser.IsOpen) return;

			browser.ServerViewer.Clear();
			browser.IsOpen = true;
			browser.DrawSplash();
			ServerMgr.Instance.Invoke(() =>
			{
				browser.IsOnline = true;
				browser.MainFeedId = 0;
				browser.Draw(onDraw: browser.DrawOverlays);
				if (Config.Sounds.PlayStartup) SendEffectTo(player, effect: "assets/prefabs/tools/keycard/effects/swipe.prefab");

				Instance.RusterAddons?.Call("RNETAPI_OnBrowserOpen", player.userID);
			}, 1.5f);
		}
		public void Launch(ulong playerId)
		{
			var browser = GetBrowser(playerId);
			if (browser.IsOpen) return;

			browser.ServerViewer.Clear();
			browser.IsOpen = true;
			browser.DrawSplash();
			ServerMgr.Instance.Invoke(() =>
			{
				browser.MainFeedId = 0;
				browser.Draw();
				Instance.RusterAddons?.Call("RNETAPI_OnBrowserOpen", playerId);
			}, 1.5f);
		}

		#endregion

		#region Bot

		public List<RusterBot> Bots { get; set; } = new List<RusterBot>();

		public void InstallBot(ulong botId, char prefix, Type type, Plugin plugin)
		{
			if (Bots.Any(x => x.Id == botId)) return;

			Bots.Add(new RusterBot(botId, prefix, type, plugin));
		}
		public void UninstallBot(ulong botId)
		{
			Bots.RemoveAll(x => x.Id == botId);
		}
		public RusterBot GetBot(ulong botId)
		{
			return Bots.FirstOrDefault(x => x.Id == botId);
		}

		public void DoBotRemovesFor(Plugin plugin)
		{
			if (Bots.Any(x => x.Plugin == plugin))
			{
				Log($"Uninstalled bot for {plugin.Name}");
			}

			Bots.RemoveAll(x => x.Plugin == plugin);
		}

		public static class CodeflingBot
		{
			public static File[] Files { get; set; }

			public class File
			{
				public int Id { get; set; }
				public string Name { get; set; }
				public string ImageUrl { get; set; }
				public string Author { get; set; }
				public string Version { get; set; }
				public string Price { get; set; }
			}

			public static void Refresh()
			{
				Instance.webrequest.Enqueue("https://codefling.com/capi/category-2/?do=apicall", string.Empty, (int code, string data) =>
				{
					Files = JsonConvert.DeserializeObject<JObject>(data)["file"].Select(x => new File
					{
						Id = x["file_id"].ToObject<string>().ToInt(),
						Name = x["file_name"]?.ToString(),
						Author = x["file_author"]?.ToObject<string>(),
						ImageUrl = x["file_image"]?["url"]?.ToObject<string>(),
						Version = x["file_version"]?.ToObject<string>(),
						Price = x["file_price"]?.ToString() == "{}" ? "Free" : x["file_price"]?.ToString()
					}).ToArray();
				}, Instance);
			}

			public static string Search(int conversationId, ulong botId, string command, string[] arguments)
			{
				var filter = arguments.ToString(" ", " ");
				var file = Files.FirstOrDefault(x => x.Name.ToLower().Contains(filter.ToLower()) || x.Id.ToString() == filter);
				if (file == null)
				{
					return $"File '{filter}' could have not been found.";
				}

				return $"{file.Name} v{file.Version} — {file.Price}\n{file.Author}";
			}

			public static void Image(int conversationId, ulong botId, string command, string[] arguments)
			{
				var filter = arguments.ToString(" ", " ");
				var file = Files.FirstOrDefault(x => x.Name.ToLower().Contains(filter.ToLower()) || x.Id.ToString() == filter);

				var conversation = Instance.Data.GetConversation(conversationId);
				conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, botId, file == null ? $"File '{filter}' could have not been found." : string.Empty)
				{
					PhotographUrl = file?.ImageUrl
				});
			}

			public static string Trade(int conversationId, ulong botId, string command, string[] arguments)
			{
				var conversation = Instance.Data.GetConversation(conversationId);
				conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, BasePlayer.FindAwakeOrSleeping(arguments[0]).userID, "")
				{
					IsTradeRequest = true
				});

				return string.Empty;
			}
		}

		#endregion

		#region Lottery

		public RusterLotteryEvent LotteryEvent { get; set; }

		public void StartLotteryEvent()
		{
			if (LotteryEvent != null)
			{
				LotteryEvent = null;
				return;
			}

			LotteryEvent = new RusterLotteryEvent();
			LotteryEvent.StartEvent();
		}

		#endregion

		#region Commands

		#region Definitions

		public static string RusterCommandPrefix => "rusternet." + Instance.Config.UniqueId;

		public static string LanguageDialogCmd => RusterCommandPrefix + ".languagedialog";
		public static string LanguageDialogChangeCmd => LanguageDialogCmd + ".change";
		public static string LanguageDialogCloseCmd => LanguageDialogCmd + ".close";

		public static string ReadNoticeCmd => RusterCommandPrefix + ".readnotice";
		public static string CloseNoticeCmd => RusterCommandPrefix + ".closenotice";

		public static string WithdrawCmd => RusterCommandPrefix + ".withdraw";
		public static string RestockAllCmd => RusterCommandPrefix + ".restockall";
		public static string MainFeedChangeCmd => RusterCommandPrefix + ".mainfeedchange";
		public static string MainDMsCmd => RusterCommandPrefix + ".maindms";
		public static string ChangeHashtagCmd => RusterCommandPrefix + ".changehashtag";
		public static string HashtagFilterCmd => RusterCommandPrefix + ".hashtagfilter";
		public static string HashtagFilterSubmitCmd => HashtagFilterCmd + ".submit";
		public static string HashtagFilterCancelCmd => HashtagFilterCmd + ".cancel";
		public static string HashtagFilterChangeCmd => HashtagFilterCmd + ".change";

		public static string CloseCmd => RusterCommandPrefix + ".close";
		public static string LikeCmd => RusterCommandPrefix + ".like";
		public static string DislikeCmd => RusterCommandPrefix + ".dislike";
		public static string DeleteCmd => RusterCommandPrefix + ".delete";
		public static string SkinPreviewCmd => RusterCommandPrefix + ".skinpreview";

		public static string NewPostCmd => RusterCommandPrefix + ".newpost";
		public static string NewPostCloseCmd => NewPostCmd + ".close";
		public static string NewPostContentCmd => NewPostCmd + ".content";
		public static string NewPostPublishCmd => NewPostCmd + ".publish";
		public static string NewPostAddPictureCmd => NewPostCmd + ".addpicture";
		public static string NewPostAddCassetteCmd => NewPostCmd + ".addcassette";
		public static string NewPostUploadAudioCmd => NewPostCmd + ".uploadaudio";
		public static string NewPostUploadAudioCloseCmd => NewPostUploadAudioCmd + ".close";
		public static string NewPostUploadAudioTitleChangeCmd => NewPostUploadAudioCmd + ".titlechange";
		public static string NewPostUploadAudioUrlChangeCmd => NewPostUploadAudioCmd + ".urlchange";
		public static string NewPostUploadAudioStartTimeChangeCmd => NewPostUploadAudioCmd + ".starttimechange";
		public static string NewPostUploadAudioUploadCmd => NewPostUploadAudioCmd + ".upload";
		public static string NewPostAddGIFCmd => NewPostCmd + ".addgif";
		public static string NewPostLocationCmd => NewPostCmd + ".location";
		public static string NewPostPollCmd => NewPostCmd + ".poll";
		public static string NewPostPollCloseCmd => NewPostCmd + ".pollclose";
		public static string NewPostPollClearCmd => NewPostCmd + ".pollclear";
		public static string NewPostPollDurationCmd => NewPostPollAddChoiceCmd + ".duration";
		public static string NewPostPollAddChoiceCmd => NewPostPollCmd + ".addchoice";
		public static string NewPostPollAddChoiceMoveUpCmd => NewPostPollAddChoiceCmd + ".moveup";
		public static string NewPostPollAddChoiceMoveDownCmd => NewPostPollAddChoiceCmd + ".movedown";
		public static string NewPostPollAddChoiceDeleteCmd => NewPostPollAddChoiceCmd + ".delete";
		public static string NewPostSoldItemCmd => NewPostCmd + ".solditem";
		public static string NewPostSoldRemoveCmd => NewPostCmd + ".removesolditem";
		public static string NewPostSoldPriceChangeCmd => NewPostCmd + ".changesolditemprice";
		public static string NewPostSoldWholeStackCmd => NewPostCmd + ".wholestack";
		public static string NewPostRecordVoiceCmd => NewPostCmd + ".recordvoice";

		public static string StartPageCmd => RusterCommandPrefix + ".startpage";
		public static string EndPageCmd => RusterCommandPrefix + ".endpage";
		public static string NextPageCmd => RusterCommandPrefix + ".nextpage";
		public static string PrevPageCmd => RusterCommandPrefix + ".prevpage";
		public static string SetPageCmd => RusterCommandPrefix + ".setpage";

		public static string FullPostCmd => RusterCommandPrefix + ".fullpost";
		public static string CloseFullPostCmd => RusterCommandPrefix + ".closefullpost";
		public static string BuyPostCmd => RusterCommandPrefix + ".buypost";
		public static string RestockPostCmd => RusterCommandPrefix + ".restockpost";
		public static string PinPostCmd => RusterCommandPrefix + ".pinpost";
		public static string PostLikesDislikesCmd => RusterCommandPrefix + ".postlikesdislikes";
		public static string ClosePostLikesDislikesCmd => RusterCommandPrefix + ".postlikesdislikes.close";
		public static string EditPostCmd => RusterCommandPrefix + ".postedit";
		public static string VoteCmd => RusterCommandPrefix + ".vote";
		public static string UserAvatarCmd => RusterCommandPrefix + ".useravatar";

		public static string OpenContactsCmd => RusterCommandPrefix + ".contacts.open";
		public static string CloseContactsCmd => RusterCommandPrefix + ".contacts.close";
		public static string ChangeContactsFilterCmd => RusterCommandPrefix + ".contacts.filter";

		public static string NewGroupCmd => RusterCommandPrefix + ".group.create";
		public static string GroupAddCmd => RusterCommandPrefix + ".group.add";
		public static string GroupPickCmd => RusterCommandPrefix + ".group.pick";
		public static string GroupKickCmd => RusterCommandPrefix + ".group.kick";

		public static string FleaMarketCmd => RusterCommandPrefix + ".fleamarket";

		public static string PlayPostCmd => RusterCommandPrefix + ".playpost";
		public static string StopPostCmd => RusterCommandPrefix + ".stoppost";
		public static string PlayMessageCmd => RusterCommandPrefix + ".playmessage";
		public static string PinToggleCmd => RusterCommandPrefix + ".pintoggle";
		public static string PinNotificationTrayToggleCmd => RusterCommandPrefix + ".pinnttoggle";
		public static string ReadNotificationCmd => RusterCommandPrefix + ".readnotification";
		public static string ReadAllNotificationsCmd => RusterCommandPrefix + ".readallnotifications";
		public static string DeleteNotificationCmd => RusterCommandPrefix + ".deletenotification";

		public static string OpenServerViewerCmd => RusterCommandPrefix + ".serverviewer.open";
		public static string CloseServerViewerCmd => RusterCommandPrefix + ".serverviewer.close";
		public static string OpenViewedServerCmd => RusterCommandPrefix + ".serverviewer.openvs";
		public static string CloseViewedServerCmd => RusterCommandPrefix + ".serverviewer.closevs";

		public static string OpenPictureViewerCmd => RusterCommandPrefix + ".pictureviewer.open";
		public static string ClosePictureViewerCmd => RusterCommandPrefix + ".pictureviewer.close";

		public static string OpenPostGifPanelCmd => RusterCommandPrefix + ".gifpanel.openpost";
		public static string CloseGifPanelCmd => RusterCommandPrefix + ".gifpanel.close";

		public static string ProfileCmd => RusterCommandPrefix + ".profile";
		public static string CloseProfileCmd => RusterCommandPrefix + ".closeprofile";
		public static string CreateProfileCardCmd => RusterCommandPrefix + ".createprofilecard";

		public static string AddFriendCmd => RusterCommandPrefix + ".addfriend";
		public static string CancelFriendRequestCmd => RusterCommandPrefix + ".cancelfriendrequest";
		public static string HandleFriendRequestCmd => RusterCommandPrefix + ".acceptfriendrequest";

		public static string RemoveFriendCmd => RusterCommandPrefix + ".removefriend";
		public static string BlockCmd => RusterCommandPrefix + ".blockedtonite";
		public static string DMCmd => RusterCommandPrefix + ".dm";
		public static string TradeCmd => RusterCommandPrefix + ".trade";
		public static string OpenUsersCmd => RusterCommandPrefix + ".users.open";
		public static string CloseUsersCmd => RusterCommandPrefix + ".users.close";
		public static string SelectUserUsersCmd => RusterCommandPrefix + ".users.select";

		public static string OpenStoryCmd => RusterCommandPrefix + ".openstory";
		public static string CloseStoryCmd => RusterCommandPrefix + ".closestory";
		public static string CreateStoryCmd => RusterCommandPrefix + ".createstory";
		public static string DeleteStoryCmd => RusterCommandPrefix + ".deletestory";

		public static string OpenTextEditorCmd => RusterCommandPrefix + ".opentexteditor";
		public static string CloseTextEditorCmd => RusterCommandPrefix + ".closetexteditor";
		public static string TextEditorContentCmd => RusterCommandPrefix + ".texteditorcontent";

		public static string ChangeConversationCmd => RusterCommandPrefix + ".conversation";
		public static string ConversationMessageChangeCmd => ChangeConversationCmd + ".messagechange";
		public static string ConversationSendCmd => ChangeConversationCmd + ".send";
		public static string ConversationSendLocationCmd => ChangeConversationCmd + ".sendlocation";
		public static string ConversationMessageDeleteCmd => ChangeConversationCmd + ".messagedelete";
		public static string ConversationDeleteCmd => ChangeConversationCmd + ".delete";
		public static string ConversationTradeCmd => RusterCommandPrefix + ".messagetrade";

		public static string ChangeMessageReactionCmd => RusterCommandPrefix + ".changemessagereaction";
		public static string UpdateMessageReactionCmd => RusterCommandPrefix + ".updatemessagereaction";
		public static string CloseMessageReactionCmd => RusterCommandPrefix + ".closemessagereaction";

		public static string AcceptConfirmDialogCmd => RusterCommandPrefix + ".acceptconfirmdialog";
		public static string CloseConfirmDialogCmd => RusterCommandPrefix + ".closeconfirmdialog";

		public static string ConfigPushNotificationsCmd => RusterCommandPrefix + ".notifications";
		public static string ConfigFriendsNotificationsCmd => RusterCommandPrefix + ".fnnotifications";
		public static string ConfigRustPlusNotificationsCmd => RusterCommandPrefix + ".rpnotifications";
		public static string ConfigDMNotificationsCmd => RusterCommandPrefix + ".dmnotifications";

		public static string CurrentStackChangeCmd => RusterCommandPrefix + ".currentstack";

		public static string OpenUserSettingsCmd => RusterCommandPrefix + ".usersettings.open";
		public static string CloseUserSettingsCmd => RusterCommandPrefix + ".usersettings.close";
		public static string UpdateUserSettingsCmd => RusterCommandPrefix + ".usersettings.update";

		public static string OpenStoreCmd => RusterCommandPrefix + ".store.open";

		public static string CloseColorPickerCmd => RusterCommandPrefix + ".colorpicker.close";
		public static string PickColorPickerCmd => RusterCommandPrefix + ".colorpicker.pick";

		public static string OpenCouponListCmd => RusterCommandPrefix + ".couponlist.open";
		public static string CloseCouponListCmd => RusterCommandPrefix + ".couponlist.close";
		public static string AddCouponListCmd => RusterCommandPrefix + ".couponlist.add";
		public static string RemoveCouponListCmd => RusterCommandPrefix + ".couponlist.remove";

		public static string OpenCouponEditorCmd => RusterCommandPrefix + ".couponeditor.open";
		public static string CloseCouponEditorCmd => RusterCommandPrefix + ".couponeditor.close";
		public static string SaveCouponEditorCmd => RusterCommandPrefix + ".couponeditor.save";
		public static string ClearCouponEditorCmd => RusterCommandPrefix + ".couponeditor.clear";
		public static string SetOptionCouponEditorCmd => RusterCommandPrefix + ".couponeditor.setoption";

		public static string SelectUserPhotoListCmd => RusterCommandPrefix + ".userphotolist.select";
		public static string CloseUserPhotoListCmd => RusterCommandPrefix + ".userphotolist.close";
		public static string OpenBannerListCmd => RusterCommandPrefix + ".bannerlist.open";
		public static string OpenAvatarListCmd => RusterCommandPrefix + ".avatarlist.open";
		public static string OpenFrameListCmd => RusterCommandPrefix + ".framelist.open";

		public static string OpenTransactionListCmd => RusterCommandPrefix + ".transactionlist.open";
		public static string CloseTransactionListCmd => RusterCommandPrefix + ".transactionlist.close";
		public static string SwitchViewTransactionListCmd => RusterCommandPrefix + ".transactionlist.switchview";
		public static string RemoveTransactionListCmd => RusterCommandPrefix + ".transactionlist.remove";

		public static string EditAboutMeCmd => RusterCommandPrefix + ".aboutme.edit";

		public static string ReportPostCmd => RusterCommandPrefix + ".reportpost";
		public static string ReportUserCmd => RusterCommandPrefix + ".reportuser";

		public static string OpenGiftBasketCmd => RusterCommandPrefix + ".giftbasket.open";

		public static string ModalPanelCmd => RusterCommandPrefix + ".modal";
		public static string CloseModalCmd => ModalPanelCmd + ".close";
		public static string SubmitModalCmd => ModalPanelCmd + ".submit";
		public static string ValueModalCmd => ModalPanelCmd + ".value";
		public static string ResetValueModalCmd => ModalPanelCmd + ".resetvalue";

		#endregion

		[ChatCommand("rusterblackmarket")]
		private void RusterBlackmarket(BasePlayer player, string command, string[] args)
		{
			if (player == null || !HasPermission(player, LaunchPerm)) return;

			var browser = GetBrowser(player);
			browser.IsOnline = Monuments.Any(x =>
				x.displayPhrase.english.ToLower().Contains("underwater") &&
				Vector3.Distance(x.transform.position, player.transform.position) <= 100f) &&
				!player.IsOutside();

			if (browser.IsOpen) return;

			browser.ServerViewer.Clear();
			browser.IsOpen = true;
			browser.DrawSplash();
			ServerMgr.Instance.Invoke(() =>
			{
				browser.DrawCustom(BlackmarketFeedId, RedRoomFeedId);
				if (Config.Sounds.PlayStartup) SendEffectTo(player, effect: "assets/prefabs/tools/keycard/effects/swipe.prefab");
			}, 1.5f);
		}

		private void Ruster(BasePlayer player, string command, string[] args)
		{
			if (!HasPermission(player, AdminPerm)) return;

			var browser = GetBrowser(args[0].ToUlong());
			browser.Player = player;
			browser.Draw();
		}
		private void RusterLogin(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);

			if (!user.IsAdmin())
			{
				Print($"You're not an administrator.", player);
				return;
			}

			var targetPlayer = BasePlayer.FindAwakeOrSleeping(args.ToString(" ", " "));
			var browser = GetBrowser(targetPlayer);
			browser.Player = player;
			browser.Draw();
		}
		private void LaunchRuster(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null || !HasPermission(player, LaunchPerm)) return;

			Launch(player);
		}
		private void CloseRuster(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null || !HasPermission(player, LaunchPerm)) return;

			var browser = GetBrowser(player);
			browser.CloseFully();
		}
		private void LaunchRuster(BasePlayer player, string command, string[] args)
		{
			if (!HasPermission(player, LaunchPerm)) return;

			Launch(player);
		}
		private void GetRuster(BasePlayer player, string command, string[] args)
		{
			if (!HasPermission(player, GetRusterPerm)) return;

			var item = ItemManager.CreateByName("paper", skin: Instance.GetProConfig(nameof(RusterSkinId), RusterSkinId));
			item.name = Instance.GetProConfig(nameof(RusterSkinName), RusterSkinName);
			player.GiveItem(item);
		}
		private void GetRuster24hAdvert(BasePlayer player, string command, string[] args)
		{
			if (!HasPermission(player, Get24hAdvertPerm)) return;

			var item = ItemManager.CreateByName("paper", skin: Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinId), RusterMarketplace24hAdvertSkinId));
			item.name = Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinName), RusterMarketplace24hAdvertSkinName);
			player.GiveItem(item);
		}
		private void GetRuster1wAdvert(BasePlayer player, string command, string[] args)
		{
			if (!HasPermission(player, Get1wAdvertPerm)) return;

			var item = ItemManager.CreateByName("paper", skin: Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinId), RusterMarketplace1wAdvertSkinId));
			item.name = Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinName), RusterMarketplace1wAdvertSkinName);
			player.GiveItem(item);
		}

		[ChatCommand("migratetosql")]
		private void MigrateToSQL(BasePlayer player, string command, string[] args)
		{
			if (player == null || !HasPermission(player, AdminPerm)) return;

			if (Config.DataType == RootConfig.DataTypes.SQL)
			{
				Print($"The database has already been migrated to SQL.");
				return;
			}

			if (DataFile.Exists())
			{
				InitializeSQL();
				SaveSQL();
				Config.DataType = RootConfig.DataTypes.SQL;

				Print($"Successfully migrated the local JSON database to SQL.");
			}
			else
			{
				Print($"There is no valid database in your <color=orange>oxide/data</color> folder to migrate to SQL.");
			}
		}

		private void RusterAllNotifications(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);

			user.Configuration.PushNotifications = args.Length == 0 ? !user.Configuration.PushNotifications : args[0].ToBool();
			user.Configuration.RustPlusNotifications = args.Length == 0 ? !user.Configuration.RustPlusNotifications : args[0].ToBool();

			Print($"You've {(args.Length == 0 ? "toggled" : (args[0].ToBool() ? "enabled" : "disabled"))} all Ruster.NET notifications.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void RusterPushNotifications(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);
			user.Configuration.PushNotifications = args.Length == 0 ? !user.Configuration.PushNotifications : args[0].ToBool();

			Print($"You've {(user.Configuration.PushNotifications ? "enabled" : "disabled")} Ruster.NET push notifications.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void RusterFriendsNotifications(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);
			user.Configuration.FriendsNotifications = args.Length == 0 ? !user.Configuration.FriendsNotifications : args[0].ToBool();

			Print($"You've {(user.Configuration.FriendsNotifications ? "enabled" : "disabled")} Ruster.NET Friends notifications.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void RusterRustPlusNotifications(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);
			user.Configuration.RustPlusNotifications = args.Length == 0 ? !user.Configuration.RustPlusNotifications : args[0].ToBool();

			Print($"You've {(user.Configuration.RustPlusNotifications ? "enabled" : "disabled")} Ruster.NET Rust+ notifications.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void RusterChatNotifications(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);
			user.Configuration.ChatNotifications = args.Length == 0 ? !user.Configuration.ChatNotifications : args[0].ToBool();

			Print($"You've {(user.Configuration.ChatNotifications ? "enabled" : "disabled")} Ruster.NET chat notifications.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void PinRusterFM(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);
			user.Configuration.PinAudioPlayer = args.Length == 0 ? !user.Configuration.PinAudioPlayer : args[0].ToBool();

			Print($"You've {(user.Configuration.PinAudioPlayer ? "pinned" : "unpinned")} Ruster.FM.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void RusterPrivacyMode(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);
			user.Configuration.PrivacyMode = args.Length == 0 ? !user.Configuration.PrivacyMode : args[0].ToBool();

			Print($"You've {(user.Configuration.PrivacyMode ? "enabled" : "disabled")} Ruster.NET privacy-mode.", player, true);

			var browser = GetBrowser(user);
			if (browser.IsOpen) browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void RusterRatio(BasePlayer player, string command, string[] args)
		{
			var user = Data.GetUser(player);

			var previousRatio = user.Configuration.Ratio;
			user.Configuration.Ratio = args[0].ToInt();

			Print($"You've changed the aspect ratio from {previousRatio}:9 to <color=orange>{user.Configuration.Ratio}:9</color>.", player, true);
		}

		[ConsoleCommand("clearrustertc")]
		private void ClearRusterTeamConversations(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null || !HasPermission(player, AdminPerm, true)) return;

			Data.Conversations.RemoveAll(x => x.ConversationType == RusterConversation.ConversationTypes.Team);
			arg.ReplyWith("Cleared.");
		}

		public static System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
		{
			var codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders();

			foreach (var codec in codecs)
			{
				if (codec.FormatID == format.Guid) return codec;
			}

			return null;
		}
		public static byte[] GetCompressedImage(System.Drawing.Image img, long quality)
		{
			var ici = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
			var eps = new System.Drawing.Imaging.EncoderParameters(2);
			eps.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
			eps.Param[1] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)System.Drawing.Imaging.EncoderValue.CompressionLZW);

			using (var ms = new MemoryStream())
			{
				img.Save(ms, ici, eps);
				return ms.ToArray();
			}
		}
		public IEnumerator DownloadGif(string url, Action<long, byte[]> onDownloaded)
		{
			var request = UnityWebRequest.Get(url);
			request.timeout = 5;
			yield return request.SendWebRequest();

			onDownloaded?.Invoke(request.responseCode, request.downloadHandler.data);
		}

		private void LanguageDialog(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawLanguageDialog();
			browser.PlayBoop();
		}
		private void LanguageDialogChange(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.GetLanguage() != arg.Args[1])
			{
				browser.SetLanguage(arg.Args[1]);
			}
			browser.CloseLanguageDialog();
			browser.Draw();
			browser.PlayBoop();
		}
		private void LanguageDialogClose(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.CloseLanguageDialog();
			browser.PlayBoop();
		}

		private void Withdraw(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			if (browser.IsButtonCooldown()) return;

			if (user.Wallet > 0)
			{
				browser.GivePlayerCurrency(user.Wallet, overrideUserMethod: RusterUserConfiguration.PaymentMethods.Currency);

				Instance.RusterAddons?.Call("RNETAPI_OnWithdraw", user.Id, user.Wallet);
				user.Wallet = 0;

				browser.Draw();
			}
		}
		private void RestockAll(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			if (browser.IsButtonCooldown()) return;

			if (!user.CanRestockAll()) return;
			browser.FullRestockMode = true;

			browser.Close();
			browser.DrawScreenNotice(browser.GetPhrase("massrestocknotice"));
		}

		private void MainFeedChange(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.ServerViewer.Clear();

			var previousMainFeedId = browser.MainFeedId;
			browser.MainFeedId = arg.Args[1].ToUlong();

			if (previousMainFeedId != browser.MainFeedId)
			{
				foreach (var page in browser.Pages) page.Value.CurrentHashtag = null;
				browser.CustomFeed1 = browser.CustomFeed2 = 0;

				browser.Draw(RusterBrowser.PanelTypes.None);
				browser.PlayBoop();
			}
		}
		private void MainDMs(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.ServerViewer.Clear();
			browser.MainFeedId = browser.PanelType == RusterBrowser.PanelTypes.DirectMessages ? 0UL : 2UL;
			browser.Draw(browser.PanelType == RusterBrowser.PanelTypes.DirectMessages ? RusterBrowser.PanelTypes.None : RusterBrowser.PanelTypes.DirectMessages);
			browser.PlayBoop();
		}
		private void ChangeHashtag(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(arg.Args[1].ToInt());
			if (page.CurrentHashtag != null)
			{
				if (page.CurrentHashtag.FilterType == (RusterHashtag.FilterTypes)arg.Args[2].ToInt() &&
					page.CurrentHashtag.Filter == arg.Args[3]) { page.CurrentHashtag = null; }
				else
				{
					page.CurrentHashtag.Filter = arg.Args.Skip(3).ToArray().ToString(" ", " ");
					page.CurrentHashtag.FilterType = (RusterHashtag.FilterTypes)arg.Args[2].ToInt();
					page.IsCustomFilter = false;
				}
			}
			else
			{
				page.CurrentHashtag = new RusterHashtag(arg.Args.Skip(3).ToArray().ToString(" ", " "), (RusterHashtag.FilterTypes)arg.Args[2].ToInt());
				page.IsCustomFilter = false;
			}

			browser.Draw(onDraw: browser.DrawOverlays);
			browser.PlayBoop();
		}
		private void HashtagFilter(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.HashtagBrowserPage = arg.Args[1].ToInt();
			browser.DrawHashtagFilter();
			browser.PlayBoop();
		}
		private void HashtagFilterSubmit(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(browser.HashtagBrowserPage);
			if (!string.IsNullOrEmpty(browser.HashtagFilterInput))
			{
				page.CurrentHashtag = new RusterHashtag(browser.HashtagFilterInput, RusterHashtag.FilterTypes.Content);
				page.IsCustomFilter = true;

				RusterAddons?.Call("RNETAPI_OnHashtagFilter", user.Id, page.Id, page.CurrentHashtag);
			}
			else
			{
				page.CurrentHashtag = null;
				RusterAddons?.Call("RNETAPI_OnHashtagFilterClear", user.Id, page.Id);
			}

			browser.HashtagFilterInput = null;
			browser.HashtagBrowserPage = 0;
			browser.CloseHashtagFilter();
			browser.Draw(onDraw: browser.DrawOverlays);
			browser.PlayBoop();
		}
		private void HashtagFilterCancel(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.HashtagFilterInput = null;
			browser.HashtagBrowserPage = 0;
			browser.CloseHashtagFilter();
			browser.PlayBoop();
		}
		private void HashtagFilterChange(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.HashtagFilterInput = arg.Args.Skip(1)?.ToArray().ToString(" ", " ");
		}

		private void AddFriend(ConsoleSystem.Arg arg)
		{
			var sentByUser = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(sentByUser);
			if (browser.IsButtonCooldown()) return;

			var sentToUser = Data.GetUser(arg.Args[1].ToUlong());
			Data.SendFriendRequest(sentByUser, sentToUser);

			GetBrowser(sentToUser).NotifyRustPlus(browser.GetPhrase("notif_t_newfriendreq"), browser.GetPhrase("notif_s_newfriendreq", sentByUser.GetDisplayName(false, observer: sentByUser)));

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
			browser.CurrentUserId = arg.Args[1].ToUlong();
			browser.DrawOverlays();
			browser.PlayBoop();
		}
		private void CancelFriendRequest(ConsoleSystem.Arg arg)
		{
			var sentByUser = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(sentByUser);
			if (browser.IsButtonCooldown()) return;

			var sentToUser = Data.GetUser(arg.Args[1].ToUlong());
			Data.CancelFriendRequest(sentByUser, sentToUser);

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
			browser.CurrentUserId = arg.Args[1].ToUlong();
			browser.DrawOverlays();
			browser.PlayBoop();
		}
		private void HandleFriendRequest(ConsoleSystem.Arg arg)
		{
			var sentByUser = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(sentByUser);
			if (browser.IsButtonCooldown()) return;

			var isPositive = arg.Args[2].ToBool();
			var sentToUser = Data.GetUser(arg.Args[1].ToUlong());
			Data.HandleFriendRequest(isPositive, sentByUser, sentToUser);

			GetBrowser(sentToUser).NotifyRustPlus(browser.GetPhrase(isPositive ? "notif_t_acceptnewfriend" : "notif_t_declinenewfriend"), browser.GetPhrase(isPositive ? "notif_s_acceptnewfriend" : "notif_s_declinenewfriend", sentByUser.GetDisplayName(false, observer: sentToUser)));

			if (arg.Args.Length <= 3)
			{
				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
				browser.CurrentUserId = arg.Args[1].ToUlong();
				browser.DrawOverlays();
				browser.PlayBoop();
			}
			else browser.Draw();

			browser.PlayBoop();
		}
		private void RemoveFriend(ConsoleSystem.Arg arg)
		{
			var sentByUser = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(sentByUser);
			if (browser.IsButtonCooldown()) return;

			var sentToUser = Data.GetUser(arg.Args[1].ToUlong());

			browser.DrawConfirmDialog(
				title: browser.GetPhrase("dialog_t_removefriend", $"{sentToUser.GetDisplayName(observer: sentByUser)} {sentToUser.GetOnlineIcon()}"),
				subtitle: $"{browser.GetPhrase("dialog_s_removefriend")}\n{(Instance.Config.DMs.MustBeFriendsToDM ? browser.GetPhrase("dialog_s_removefriend_1") : "")}", onAccept: () =>
				{
					Data.RemoveFriend(sentByUser, sentToUser);

					browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
					browser.CurrentUserId = arg.Args[1].ToUlong();
					browser.DrawOverlays();
				});

			browser.PlayBoop();
		}

		private void Close(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.CloseFully();
			browser.PlayBoop();
		}

		private void Like(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Instance.Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_postlikedremoved"))) return;

			// post.Ticks = new DateTime ( post.Ticks ).AddDays ( -1 ).Ticks;

			var feed = Instance.Data.GetFeed(arg.Args[2].ToUlong());

			if (user.HasBlockedCommunication(post.UserId))
			{
				browser.PlayBoop();
				if (browser.OverlayPanelType == RusterBrowser.OverlayPanelTypes.None) browser.Draw(); else browser.DrawOverlays();

				browser.Notify(browser.GetPhrase("notif_t_likeblockedcom"), browser.GetPhrase("notif_s_likeblockedcom"), playSound: false);
			}
			else
			{
				if (!post.HasLiked(user)) browser.PlayLike(); else browser.PlayBoop();
				post.Like(user);

				if (post.IsReply())
				{
					browser.RedrawAllFullPosts();
				}
				else if (!browser.AlreadyDrawingFullPost(post)) { browser.Draw(browser.CustomFeed1 != 0 || browser.CustomFeed2 != 0 ? RusterBrowser.PanelTypes.CustomFeeds : RusterBrowser.PanelTypes.None); }
				else
				{
					browser.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
					{
						browser.DrawFullPost(feed, post);
					});
				}

				if (post.UserId != user.Id) GetBrowser(post.UserId).NotifyRustPlus(browser.GetPhrase("notif_t_liked", user.GetDisplayName(false, observer: post.GetUser()), post.GetPostType().ToLower()), $"{post.Content}");
			}
		}
		private void Dislike(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Instance.Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_postdislikedremoved"))) return;

			var feed = Instance.Data.GetFeed(arg.Args[2].ToUlong());

			if (user.HasBlockedCommunication(post.UserId))
			{
				browser.PlayBoop();
				if (browser.OverlayPanelType == RusterBrowser.OverlayPanelTypes.None) browser.Draw(browser.CustomFeed1 != 0 || browser.CustomFeed2 != 0 ? RusterBrowser.PanelTypes.CustomFeeds : RusterBrowser.PanelTypes.None); else browser.DrawOverlays();

				browser.Notify(browser.GetPhrase("notif_t_dislikeblockedcom"), browser.GetPhrase("notif_s_dislikeblockedcom"), playSound: false);
			}
			else
			{
				if (!post.HasDisliked(user)) browser.PlayDislike(); else browser.PlayBoop();
				post.Dislike(user);

				if (post.IsReply())
				{
					browser.RedrawAllFullPosts();
				}
				else if (!browser.AlreadyDrawingFullPost(post)) { browser.Draw(browser.CustomFeed1 != 0 || browser.CustomFeed2 != 0 ? RusterBrowser.PanelTypes.CustomFeeds : RusterBrowser.PanelTypes.None); }
				else
				{
					browser.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
					{
						browser.DrawFullPost(feed, post);
					});
				}

				if (post.UserId != user.Id) GetBrowser(post.UserId).NotifyRustPlus(browser.GetPhrase("notif_t_disliked", user.GetDisplayName(false, observer: post.GetUser()), post.GetPostType().ToLower()), $"{post.Content}");
			}
		}
		private void Vote(ConsoleSystem.Arg arg)
		{
			var user = Instance.Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Instance.Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_postvotedremoved"))) return;

			var feed = Instance.Data.GetFeed(arg.Args[2].ToUlong());

			post.Poll.Choices[arg.Args[3].ToInt()].Votes.Add(user.Id);
			browser.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
			{
				browser.DrawFullPost(feed, post);
			});
		}
		private void SkinPreview(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawPictureViewer(arg.Args[1], 0.2175f, 0.1f);
			browser.PlayBoop();
		}

		private void NewPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (!arg.Args[1].ToUlong().IsSteamId() && RusterFeed.RusterPost.IsValidPostId(arg.Args[1].ToInt()))
			{
				var post = Data.GetPost(arg.Args[1].ToInt());
				if (HasFailed(browser, post == null, browser.GetPhrase("er_postreplyremoved"))) return;
			}

			if (arg.Args.Length > 1) browser.FeedId = arg.Args[1].ToUlong();

			var feed = Data.GetFeed(browser.FeedId);
			if (feed.GetFeedType() == RusterFeed.FeedTypes.Shop) browser.MarketplaceItem = new RusterMarketplaceListing() { WholeStack = Config.Marketplace.ValidateWholeStack(true) };

			browser.WholeStack = Config.Marketplace.ValidateWholeStack(true);
			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.Draw(RusterBrowser.PanelTypes.NewPost);

			browser.PlayBoop();
		}
		private void NewPostClose(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.OverlayPanelType == RusterBrowser.OverlayPanelTypes.None)
				browser.Draw(
					browser.CustomFeed1 != 0 || browser.CustomFeed2 != 0 ? RusterBrowser.PanelTypes.CustomFeeds : RusterBrowser.PanelTypes.None,
					onDraw: browser.DrawOverlays);

			browser.PlayBoop();

			if (!string.IsNullOrEmpty(browser.MarketplaceItem?.Shortname))
			{
				var item = browser.MarketplaceItem?.CreateItem();
				if (item != null) user.GetPlayer()?.GiveItem(item);
			}

			browser.SellingContainer?.Kill();
			browser.PhotographContainer?.Kill();
			browser.SellingContainer = browser.PhotographContainer = null;
			browser.MarketplaceItem = null;
			browser.Content = browser.UploadedPhotograph = browser.UploadedPhotographTag = browser.UploadedCassetteTitle = null;
			browser.UploadedCassetteId = 0;
		}
		private void NewPostContent(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var content = arg.Args.Skip(1).ToArray().ToString(" ", " ");
			if (!string.IsNullOrEmpty(content)) browser.Content = content;
		}
		private void NewPostPublish(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.Poll != null && browser.Poll.Choices.Count < 2)
			{
				browser.Notify("Cannot publish", "There must be between 2-7 choices in the poll to be able to publish.");
				return;
			}

			if (!arg.Args[1].ToUlong().IsSteamId() && RusterFeed.RusterPost.IsValidPostId(arg.Args[1].ToInt()))
			{
				var replyPost = Data.GetPost(arg.Args[1].ToInt());
				if (HasFailed(browser, replyPost == null, browser.GetPhrase("er_postpublishremoved"))) return;
			}

			var content = browser.Content?.Replace("\\n", "").EscapeRichText();
			var feed = arg.Args.Length > 1 ? Instance.Data.GetFeed(arg.Args[1].ToUlong()) : null;
			var post = RusterFeed.RusterPost.Create(browser.User, feed.EnableCensorship ? Instance.Config.Profanity.DoProfanityCheck(content) : content);
			var player = user.GetPlayer();

			if (string.IsNullOrEmpty(content))
			{
				browser.Notify(browser.GetPhrase("notif_t_notpublishednocontent"), browser.GetPhrase("notif_s_notpublishednocontent"));
				return;
			}

			if (browser.IsAdvertPost)
			{
				post.Advert = new RusterFeed.RusterPost.RusterAdvert
				{
					DurationHours = browser.Is24hAdvert ? 24 : 24 * 7
				};
			}
			post.PhotoUrl = browser.UploadedPhotograph;
			post.PhotoTag = feed.EnableCensorship ? Config.Profanity.DoProfanityCheck(browser.UploadedPhotographTag) : browser.UploadedPhotographTag;
			post.CassetteId = browser.UploadedCassetteId;
			post.CassetteTitle = browser.UploadedCassetteTitle;
			post.Poll = browser.Poll;

			if (browser.PostGif != null)
			{
				post.Gif = new RusterFeed.RusterPost.RusterGif
				{
					Id = browser.PostGif.Id,
					Frames = browser.PostGif.Frames,
					IsFlipbook = browser.PostGif.IsFlipbook,
					Url = browser.PostGif.Url
				};
			}

			if (browser.Location)
			{
				post.Location.Name = browser.GetNearbyMonument()?.displayPhrase.english;
				post.Location.Position = new RusterFeed.RusterPost.RusterVector3(player == null ? Vector3.one : player.transform.position);
			}
			if (browser.MarketplaceItem != null)
			{
				post.MarketplaceListing = browser.MarketplaceItem;

				if (!(user.IsAdmin() || user.IsModerator()) && string.IsNullOrEmpty(browser.MarketplaceItem.Shortname) && feed.GetFeedType() == RusterFeed.FeedTypes.Shop)
				{
					browser.Notify(browser.GetPhrase("notif_t_notpublishednoitem"), browser.GetPhrase("notif_s_notpublishednoitem"));
					return;
				}

				post.MarketplaceListing.AmountLeft = post.MarketplaceListing.Amount;
				post.MarketplaceListing.WholeStack = browser.WholeStack;
				post.MarketplaceListing._Item = null;

				Instance.GetSteamWorkshopIcon(post.MarketplaceListing.Skin, url =>
				{
					post.MarketplaceListing.SkinIconUrl = url;
				});

				if (string.IsNullOrEmpty(browser.MarketplaceItem.Shortname)) post.MarketplaceListing = null;
			}

			post.Publish(feed);

			if (feed.GetFeedType() == RusterFeed.FeedTypes.Post)
			{
				var feedPost = Data.GetPost((int)feed.Id);
				var feedPostUser = feedPost.GetUser();
				var feedPostBrowser = GetBrowser(feedPostUser);
				feedPostBrowser.NotifyRustPlus(browser.GetPhrase("newpostreply"), $"{user.GetDisplayName(observer: feedPostUser)}: {post.Content}");
			}

			browser.FeedId = feed.Id;
			browser.PostId = post.Id;
			browser.Draw(browser.CustomFeed1 != 0 || browser.CustomFeed2 != 0 ? RusterBrowser.PanelTypes.CustomFeeds : RusterBrowser.PanelTypes.None, onDraw: () =>
			{
				browser.DrawOverlays();
				browser.DrawFullPost(feed, post);
			});

			browser.PlayBoop();

			browser.WholeStack = true;
			browser.IsAdvertPost = false;
			browser.Poll = null;
			browser.PostGif = null;
			browser.SellingContainer?.Kill();
			browser.PhotographContainer?.Kill();
			browser.SellingContainer = browser.PhotographContainer = null;
			browser.MarketplaceItem = null;
			browser.Content = browser.UploadedPhotograph = browser.UploadedPhotographTag = browser.UploadedCassetteTitle = null;
			browser.UploadedCassetteId = 0;

			if (player != null && post.IsAdvert())
			{
				var skin = browser.Is24hAdvert ? Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinId), RusterMarketplace24hAdvertSkinId) : Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinId), RusterMarketplace1wAdvertSkinId);
				if (!browser.TakeStorageItems("paper", 1, player.inventory.containerBelt, skin))
					browser.TakeStorageItems("paper", 1, player.inventory.containerMain, skin);

				player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain, false);
				player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, player.inventory.containerBelt, false);
			}
		}
		private void NewPostAddPicture(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (string.IsNullOrEmpty(Config.PhotographUpload.ImgurClientId))
			{
				browser.Notify(browser.GetPhrase("notif_t_noimgurclient"), browser.GetPhrase("notif_s_noimgurclient", Name));
				browser.PlayBoop();
				return;
			}

			browser.Close();

			browser.UploadingPhotograph = true;
			browser.PhotographContainer = browser.CreateContainer(null, canAcceptItem: (item, amount) => { return item.info.shortname == "photo" || item.skin == RusterShortFlipbookSkinId || item.skin == RusterMediumFlipbookSkinId || item.skin == RusterLongFlipbookSkinId; });
			browser.OpenPhotographPanel(arg.Player());

			browser.PlayBoop();
		}
		private void NewPostAddCassette(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var player = user.GetPlayer();

			if (player != null)
			{
				browser.Close();

				browser.CassetteContainer = browser.CreateContainer(null);
				browser.OpenCassettePanel(player);
			}
			browser.PlayBoop();
		}
		private void NewPostUploadAudio(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (!Config.Sounds.IsValid())
			{
				browser.Notify(browser.GetPhrase("notif_t_invalidaudio"), browser.GetPhrase("notif_s_invalidaudio"));
				return;
			}

			browser.Close();
			browser.DrawUploadAudio();
			browser.PlayBoop();
		}
		private void NewPostUploadAudioClose(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;
			browser.UploadAudioUrl = string.Empty;
			browser.UploadAudioSkip = string.Empty;

			browser.CloseUploadAudio();
			browser.Draw(onDraw: browser.DrawOverlays);
			browser.PlayBoop();
		}
		private void NewPostUploadAudioUrlChange(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.UploadAudioUrl = arg.Args.Length > 1 ? arg.Args[1].Trim() : string.Empty;
		}
		private void NewPostUploadAudioStartTimeChange(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.UploadAudioSkip = arg.Args.Length > 1 ? arg.Args[1].Trim() : string.Empty;
		}
		private void NewPostUploadAudioTitleChange(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.UploadedCassetteTitle = arg.Args.Length > 1 ? arg.Args.Skip(1).ToArray().ToString(" ", " ").Trim() : string.Empty;
		}
		private void NewPostUploadAudioUpload(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (string.IsNullOrEmpty(browser.UploadAudioUrl))
			{
				browser.Notify(browser.GetPhrase("notif_t_audiouploademptyurl"), browser.GetPhrase("notif_s_audiouploademptyurl"));

				return;
			}

			var time = string.IsNullOrEmpty(browser.UploadAudioSkip) ? string.Empty : browser.UploadAudioSkip.Trim();
			var date = DateTick.Current;
			var startMinute = 0;
			var startSecond = time.ToInt();
			var endSecond = startSecond + 30;
			var endMinute = startMinute;

			if (time.Contains(":"))
			{
				var split = time.Split(':');
				startMinute = split[0].ToInt();
				startSecond = split[1].ToInt();
				endSecond = startSecond + 30;
				endMinute = startMinute;
			}

			if (endSecond >= 60)
			{
				endSecond = 60 - startSecond;
				endMinute++;
			}

			var trimStart = $"00:{startMinute:00}:{startSecond:00}.0";
			var trimEnd = $"00:{endMinute:00}:{endSecond:00}.0";

			global::RusterNET.Core.AudioUpload.ConvertToOgg(
				url: browser.UploadAudioUrl,
				temporaryFileName: $"{GetTempFolder()}{Path.DirectorySeparatorChar}ytfile_{date.Ticks}.mp3",
				ffmpegPath: Config.Sounds.FFMPEGPath,
				trimStart: trimStart,
				trimEnd: trimEnd,
				onCompleted: (byte[] oggData, string temporaryMp3, string temporaryOgg) =>
				{
					OsEx.File.Delete(temporaryMp3);
					OsEx.File.Delete(temporaryOgg);

					if (oggData == null) return;

					var cassetteCopy = GameManager.server.CreateEntity("assets/prefabs/voiceaudio/cassette/cassette.entity.prefab") as Cassette;
					cassetteCopy.MaxCassetteLength = 30f;
					cassetteCopy.PreloadType = PreloadedCassetteContent.PreloadType.Long;
					cassetteCopy.Spawn();

					var fileId = FileStorage.server.Store(oggData, FileStorage.Type.ogg, cassetteCopy.net.ID);
					cassetteCopy.SetAudioId(fileId, user.Id);
					cassetteCopy.SendNetworkUpdate();

					browser.UploadedCassetteId = cassetteCopy.net.ID.Value;
					browser.UploadedCassetteTitle = browser.UploadedCassetteTitle;

					browser.CloseCover("upload_audio");
					browser.CloseUploadAudio();
					browser.Draw(onDraw: browser.DrawOverlays);

					var item = ItemManager.CreateByName("cassette");
					item.instanceData.subEntity = cassetteCopy.net.ID;
					item.MarkDirty();

					if (!item.MoveToContainer(browser.Player.inventory.containerMain))
						item.MoveToContainer(browser.Player.inventory.containerBelt);

					item.instanceData.subEntity = new NetworkableId(0);
					item.MarkDirty();
					item.Remove();
				},
				onFailed: (Exception exception) =>
				{
					browser.CloseCover("upload_audio");
					browser.CloseUploadAudio();
					browser.Draw(onDraw: browser.DrawOverlays);
				});

			browser.Close();
			browser.DrawCover("upload_audio", browser.GetPhrase("cov_uploadingad"), autoClose: false);

			browser.UploadAudioUrl = string.Empty;
			browser.UploadAudioSkip = string.Empty;
		}
		private void NewPostAddGIF(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawTextEditor("Add GIF", secondText: $"<b>Add GIF</b>: Add the URL for the GIF you're willing to upload.", onSubmit: content =>
			{
				var split = content.Split('/');
				var name = split[split.Length - 1];
				var id = GetStringChecksum(content);

				browser.DrawCover("uploadgif", "Uploading GIF...", false);

				ServerMgr.Instance.StartCoroutine(DownloadGif(content, (code, data) =>
				{
					if (code != 200)
					{
						browser.DrawCover("uploadgif", $"Failed GIF uploading.", closeAfter: 2f);
						return;
					}

					var datas = new Dictionary<string, byte[]>();

					try
					{
						using (var stream = new MemoryStream(data))
						{
							var frames = GetFrames(System.Drawing.Image.FromStream(stream), 2);
							var index = 0;

							for (int i = 0; i < frames.Length; i++)
							{
								var frame = frames[i];
								if (frame == null) continue;

								var image = GetCompressedImage(frame, 60);
								datas.Add($"{id}_{index}", image);
								index++;
							}
						}
					}
					catch
					{
						browser.DrawCover("uploadgif", $"Failed GIF uploading. GDI+ error.", closeAfter: 2f);
						return;
					}

					Instance.ImageLibrary.ImportImageData($"Ruster.NET GIF Uploading - {name}", datas, 0, true, new Action(() =>
					{
						var initialFrame = datas.ElementAt(RandomEx.GetRandomInteger(0, datas.Count - 1));
						var thumbnailPath = $"{GetTempFolder()}{Path.DirectorySeparatorChar}thumbnail_{name}.jpg";
						OsEx.File.Create(thumbnailPath, initialFrame.Value);

						var exception = (Exception)null;
						browser.PostGif = new RusterFeed.RusterPost.RusterGif
						{
							Frames = datas.Count,
							Url = content,
							Id = id
						};
						browser.UploadedPhotograph = global::RusterNET.Core.PhotoUpload.UploadImageToImgur(thumbnailPath, Instance.Config.PhotographUpload.ImgurClientId, out exception);
						Instance.timer.In(1f, () => { try { OsEx.File.Delete(thumbnailPath); } catch { } });

						browser.Draw(onDraw: () =>
						{
							browser.DrawCover("uploadgif", $"GIF uploaded successfully!", closeAfter: 2f);
						});
					}));
				}));

			}, onCancel: () =>
			{

			}, canSubmit: (oldContent, newContent) =>
			{
				if (string.IsNullOrEmpty(newContent)) return new KeyValuePair<bool, string>(false, "Empty content.");
				if (!newContent.StartsWith("http")) return new KeyValuePair<bool, string>(false, "The text you inserted is not a valid URL.");

				return new KeyValuePair<bool, string>(true, string.Empty);
			});

			browser.PlayBoop();
		}

		private void NewPostRecordVoice(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var player = user.GetPlayer();
			browser.Close();

			player.inventory.containerBelt.SetLocked(true);

			browser.HeldItemContainer = browser.CreateContainer(null, capacity: 15);
			foreach (var item in player.inventory.containerBelt.itemList.ToArray()) item.MoveToContainer(browser.HeldItemContainer);

			var cassetteRecorder = ItemManager.CreateByName("fun.casetterecorder");
			var cassette = ItemManager.CreateByName("cassette");
			cassette.MoveToContainer(cassetteRecorder.contents);
			cassetteRecorder.contents.SetLocked(true);

			cassetteRecorder.MoveToContainer(player.inventory.containerBelt);
			browser.TemporaryHeldItem = cassetteRecorder;

			browser.DrawScreenNotice(
				browser.GetPhrase("voicerecordnotice"),
				verticalOffset: 80);
		}
		private void NewPostLocation(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.Location = !browser.Location;

			browser.Draw();
			browser.PlayBoop();
		}
		private void NewPostPoll(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.Poll == null)
				browser.Poll = new RusterFeed.RusterPost.RusterPoll()
				{
					DurationHours = Config.Polls.DefaultDuration
				};

			browser.UploadedPhotograph = browser.UploadedPhotographTag = null;

			browser.Draw(RusterBrowser.PanelTypes.PollEditor);
			browser.PlayBoop();
		}
		private void NewPostPollClose(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (!browser.Poll.Choices.Any()) browser.Poll = null;
			else browser.UploadedPhotograph = browser.UploadedPhotographTag = null;

			browser.Draw(RusterBrowser.PanelTypes.NewPost);
			browser.PlayBoop();
		}
		private void NewPostPollClear(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.Poll = null;

			browser.Draw(RusterBrowser.PanelTypes.NewPost);
			browser.PlayBoop();
		}
		private void NewPostPollDuration(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawTextEditor(onSubmit: text =>
			{
				browser.Poll.DurationHours = text.ToFloat().Clamp(0.1f, 48f);
				browser.Draw(onDraw: () =>
				{
					browser.DrawOverlays();
				});
			}, secondText: browser.GetPhrase("poll_enterduration"), canSubmit: (main, second) =>
			{
				var value = 0f;
				if (!float.TryParse(second, out value)) return new KeyValuePair<bool, string>(false, "NaN.");

				return new KeyValuePair<bool, string>(true, null);
			}); browser.PlayBoop();
		}
		private void NewPostPollAddChoice(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.Poll.Choices.Count >= 7)
			{
				browser.Draw(onDraw: () =>
				{
					browser.DrawOverlays();
					browser.Notify(browser.GetPhrase("poll_maxchoice_t"), browser.GetPhrase("poll_maxchoice_c"));
				});
				browser.PlayBoop();
				return;
			}

			browser.DrawTextEditor(onSubmit: text =>
			{
				var choice = new RusterFeed.RusterPost.RusterPoll.Choice();

				choice.Text = text;
				browser.Poll.Choices.Add(choice);
				browser.Draw();
			}, secondText: browser.GetPhrase("poll_enterchoice"), canSubmit: (main, second) =>
			{
				if (string.IsNullOrEmpty(second)) return new KeyValuePair<bool, string>(false, browser.GetPhrase("poll_inputempty"));
				if (browser.Poll.Choices.Any(x => x.Text.ToLower().Trim() == second.ToLower().Trim())) return new KeyValuePair<bool, string>(false, browser.GetPhrase("poll_samechoice"));

				return new KeyValuePair<bool, string>(true, null);
			});

			browser.PlayBoop();
		}
		private void NewPostPollAddChoiceMoveUp(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var index = arg.Args[1].ToInt();
			var choice = browser.Poll.Choices[index];
			if (index != 0)
			{
				browser.Poll.Choices.RemoveAt(index);
				browser.Poll.Choices.Insert(index - 1, choice);
			}
			browser.Draw();
			browser.PlayBoop();
		}
		private void NewPostPollAddChoiceMoveDown(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var index = arg.Args[1].ToInt();
			var choice = browser.Poll.Choices[index];
			if (index != browser.Poll.Choices.Count - 1)
			{
				browser.Poll.Choices.RemoveAt(index);
				browser.Poll.Choices.Insert(index + 1, choice);
			}
			browser.Draw();
			browser.PlayBoop();
		}
		private void NewPostPollAddChoiceDelete(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.Poll.Choices.RemoveAt(arg.Args[1].ToInt());

			browser.Draw();
			browser.PlayBoop();
		}
		private void NewPostSoldWholeStack(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.WholeStack = Config.Marketplace.ValidateWholeStack(!browser.WholeStack);

			browser.DisableCooldown = true;
			NewPostSoldRemove(arg);
			browser.DisableCooldown = false;

			browser.PlayBoop();
		}

		private void NewPostSoldItem(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var player = user.GetPlayer();

			if (player != null)
			{
				browser.Close();

				browser.SellingContainer = browser.CreateContainer(null, maxStackSize: browser.WholeStack ? Config.Marketplace.MaximumStackSizeWholeStack : Config.Marketplace.MaximumStackSizeEachItem);
				browser.SellingContainer.canAcceptItem = (Item item, int amount) =>
				{
					if (item.skin == RusterSkinId || item.skin == RusterMarketplace24hAdvertSkinId || item.skin == RusterMarketplace1wAdvertSkinId || item.skin == RusterBusinessCardSkinId)
						return false;

					return true;
				};
				browser.OpenSoldItemPanel(player);
			}
			browser.PlayBoop();
		}
		private void NewPostSoldRemove(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var item = browser.MarketplaceItem?.CreateItem();
			if (browser.MarketplaceItem != null && item != null) user.GetPlayer()?.GiveItem(item);

			if (browser.MarketplaceItem != null) browser.MarketplaceItem.Clear();
			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.Draw(onDraw: browser.DrawOverlays);
		}
		private void NewPostSoldPriceChange(ConsoleSystem.Arg arg)
		{
			if (arg.Args.Length > 1)
			{
				var user = Data.GetUser(arg.Args[0].ToUlong());
				var browser = GetBrowser(user);
				browser.Close();

				browser.MarketplaceItem.Price = arg.Args[1].ToInt().Clamp(Instance.Config.Marketplace.MinimumPrice, Instance.Config.Marketplace.MaximumPrice);

				browser.Draw(onDraw: browser.DrawOverlays);
			}
		}

		private void FullPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.ServerViewer.CurrentServer == null && HasFailed(browser, Data.GetPost(arg.Args[2].ToInt()) == null, browser.GetPhrase("er_postfullyremoved"))) return;

			var feed = (RusterFeed)null;
			var post = (RusterFeed.RusterPost)null;
			var feedId = arg.Args[1].ToUlong();
			var postId = arg.Args[2].ToInt();

			if (browser.ServerViewer.IsViewing())
			{
				feed = browser.ServerViewer.CommunityFeed.Id == feedId ? browser.ServerViewer.CommunityFeed : browser.ServerViewer.MarketplaceFeed;
				post = feed.GetPost(postId);
			}
			else
			{
				post = Data.GetPost(postId);
				feed = post.GetFeed() ?? Data.GetFeed(feedId);
			}

			browser.DrawFullPost(feed, post);
			browser.PlayBoop();

			Instance.RusterAddons?.Call("RNETAPI_OnPostOpen", user.Id, post);
		}
		private void CloseFullPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.CurrentUserId != 0) browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
			else browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;

			var feed = (RusterFeed)null;
			var post = (RusterFeed.RusterPost)null;
			var feedId = arg.Args[1].ToUlong();
			var postId = arg.Args[2].ToInt();

			if (browser.ServerViewer.IsViewing()) feed = browser.ServerViewer.CommunityFeed.Id == feedId ? browser.ServerViewer.CommunityFeed : browser.ServerViewer.MarketplaceFeed; else feed = Data.GetFeed(feedId);
			post = feed.GetPost(postId);

			browser.CloseFullPost(feed, post);
			browser.PlayBoop();

			Instance.RusterAddons?.Call("RNETAPI_OnPostClose", user.Id, post);
		}
		private void BuyPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown() || browser.ServerViewer.IsViewing()) return;

			var post = Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_postpurchaseremoved")) || !post.CanBuy(user)) return;

			if (user.Configuration.GiftTarget != null && !user.Configuration.GiftTarget.CanAcceptGifts())
			{
				browser.Notify("Purchase Failure", $"{user.Configuration.GiftTarget.GetDisplayName()} cannot accept gifts at this current moment in time.");
				return;
			}

			switch (post.Id)
			{
				case 930:
					if (post.MarketplaceListing.Price == 0)
					{
						browser.DrawTextEditor(
							"Custom Gift Card",
							secondText: "<b>Custom Gift Card</b>: Enter the value of the Gift Card. You must have the amount in your wallet / server currency.",
							canSubmit: (oldText, newText) =>
							{
								var value = 0;
								if (!int.TryParse(newText, out value))
								{
									return new KeyValuePair<bool, string>(false, "Not a number.");
								}

								if (!browser.PlayerHasCurrency(value) || value < 5)
								{
									return new KeyValuePair<bool, string>(false, $"You may use a value between {Instance.Config.Currency.GetValueName(browser, 5, true)} and {Instance.Config.Currency.GetValueName(browser, (int)browser.GetPlayerCurrency(), true)}.");
								}

								return new KeyValuePair<bool, string>(true, string.Empty);
							},
							onSubmit: (content) =>
							{
								var price = content.ToInt();
								post.MarketplaceListing.Price = price;
								post.MarketplaceListing.Text = JsonConvert.SerializeObject(new RusterGiftCard(user, price));
								BuyPost(arg);
							});

						return;
					}
					break;
			}

			var originalPrice = post.MarketplaceListing.GetPrice(browser);
			var sellPrice = post.MarketplaceListing.GetTaxedPrice(browser, add: false);
			var receivedPrice = originalPrice - (sellPrice - originalPrice);
			var canGetForFree = post.CanGetForFree(user);
			var player = user.GetPlayer();
			var coupon = post.MarketplaceListing.Coupon;
			var canApplyCoupon = coupon != null && !coupon.IsDepleted() && user.Configuration.Coupons.Contains(coupon.Code);
			var hasUsedCoupon = false;
			var targetUser = user.GetTargetUser();

			if (IsInFleaMarket(browser))
			{
				if (!browser.PlayerHasCurrency(sellPrice))
				{
					browser.DrawCustom(FleaMarketId);
					player.Invoke(() => browser.Notify(browser.GetPhrase("notif_t_notenoughcurrency", Instance.Config.Currency.GetName(browser)), browser.GetPhrase("notif_s_notenoughcurrency", Instance.Config.Currency.GetValueName(browser, sellPrice, true)), playSound: false), 0.75f);
					return;
				}

				webrequest.Enqueue(FleaBuyRoute, $@"{{ ""sessionId"": ""{(global::RusterNET.Core.Internet.GetSessionId())}"", ""id"": {post.Id}, ""amount"": {browser.CurrentStackAmount}, ""userId"": {user.Id} }}",
				(int statusCode, string data) =>
				{
					if (statusCode != 200)
					{
						browser.Notify("Cannot Purchase", "This server is not verified, hence why Flea Market purchases are not available.", 10f);
						browser.Draw();
						return;
					}

					browser.DrawCover("fleapurchase", "Processing transaction...", false);

					FetchFleaMarket(passed =>
					{
						if (!canGetForFree) browser.TakePlayerCurrency(sellPrice);
						player?.GiveItem(post.MarketplaceListing.CreateItem(browser.CurrentStackAmount));

						browser.CloseCover("fleapurchase");
						if (!passed) { browser.Draw(onDraw: browser.DrawOverlays); return; }

						browser.RedrawAllFullPosts();

						browser.Notify(browser.GetPhrase("notif_t_purchase"), browser.GetPhrase("notif_s_purchase"), onRead: b =>
						{
							b.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
							{
								b.DrawFullPost(post.GetFeed(), post);
							});
						}, openName: "Post");

						var postUser = Data.GetUser(post.UserId);
						Data.RegisterPurchase(user, new RusterTransaction(postUser, post.MarketplaceListing, post.MarketplaceListing.WholeStack ? post.MarketplaceListing.Amount : browser.CurrentStackAmount, sellPrice, hasUsedCoupon));
					});
				}, this, RequestMethod.POST);

				return;
			}

			var purchaseAction = new Action(() =>
			{
				switch (post.Id)
				{
					case 930:
						post.MarketplaceListing.Price = 0;
						break;
				}

				if (hasUsedCoupon)
				{
					sellPrice = coupon.GetDiscountedPrice(sellPrice);
					receivedPrice = coupon.GetDiscountedPrice(receivedPrice);
					coupon.DoUse(user);
				}

				if (!post.MarketplaceListing.WholeStack && post.MarketplaceListing.AmountLeft > 0 && browser.CurrentStackAmount <= 0) browser.CurrentStackAmount = 1;
				var item = post.MarketplaceListing.GetSoldItemDefinition();
				var itemInfo = $"{post.MarketplaceListing.GetAmount(browser):n0} x {(string.IsNullOrEmpty(post.MarketplaceListing.CustomName) ? browser.GetPhrase(item) : post.MarketplaceListing.CustomName)}";

				post = Data.GetPost(arg.Args[1].ToInt());
				if (HasFailed(browser, post == null, browser.GetPhrase("er_postpurchaseremoved"))) return;

				var postUser = Data.GetUser(post.UserId);
				post.MarketplaceListing._Item = null;

				if (!post.MarketplaceListing.IsPurchased)
				{
					if (Config.License.IsLicensedItem(post.PhotoUrl) && Data.HasLicense(targetUser.Id, post.PhotoUrl))
					{
						browser.Notify("Already Owned", "You already own a license for this item.", playSound: false);
						return;
					}
					else if (canGetForFree || browser.PlayerHasCurrency(sellPrice))
					{
						ServerMgr.Instance.Invoke(() =>
						{
							if (post.UserId != user.Id && !postUser.IsBot)
							{
								var postUserBrowser = GetBrowser(postUser);
								postUserBrowser.Notify(postUserBrowser.GetPhrase("notif_t_bought", user.GetDisplayName(true, observer: postUser), itemInfo), postUserBrowser.GetPhrase("notif_s_bought", Instance.Config.Currency.GetValueName(postUserBrowser, sellPrice, true), post.Content), 12.5f, onRead: b =>
								{
									b.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
									{
										b.DrawFullPost(post.GetFeed(), post);
									});
								}, openName: "Post");
								postUserBrowser.NotifyRustPlus(postUserBrowser.GetPhrase("notif_t_bought", user.GetDisplayName(false, observer: postUser), itemInfo), postUserBrowser.GetPhrase("notif_s_bought", Instance.Config.Currency.GetValueName(postUserBrowser, sellPrice, false), post.Content), forceSend: true);
							}

							browser.Notify(browser.GetPhrase("notif_t_purchase"), browser.GetPhrase("notif_s_purchase"), onRead: b =>
							{
								b.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
								{
									b.DrawFullPost(post.GetFeed(), post);
								});
							}, openName: "Post");
						}, 0.2f);

						if (!canGetForFree)
						{
							browser.TakePlayerCurrency(sellPrice);
							Data.Stock.Value += Mathf.Abs(sellPrice - originalPrice);
						}

						var license = (RusterLicensedItem)null;
						if (Config.License.IsLicensedItem(post.PhotoUrl, out license))
						{
							Data.RegisterLicensedItem(targetUser, license);
						}
						else
						{
							if (targetUser == user)
							{
								player?.GiveItem(post.MarketplaceListing.CreateItem(browser.CurrentStackAmount));
							}
							else
							{
								Data.RegisterGift(targetUser, post.MarketplaceListing, browser.CurrentStackAmount);
							}
						}

						if (!postUser.IsBot)
						{
							if (post.MarketplaceListing.WholeStack) post.MarketplaceListing.IsPurchased = true;
							else
							{
								post.MarketplaceListing.AmountLeft -= browser.CurrentStackAmount;
								post.MarketplaceListing.AmountLeft = post.MarketplaceListing.AmountLeft.Clamp(0, post.MarketplaceListing.Amount);
								if (post.MarketplaceListing.AmountLeft == 0)
									post.MarketplaceListing.IsPurchased = true;
							}
						}

						if (!canGetForFree) post.GetUser().Wallet += receivedPrice;
					}
					else
					{
						browser.Notify(browser.GetPhrase("notif_t_notenoughcurrency", Instance.Config.Currency.GetName(browser)), browser.GetPhrase("notif_s_notenoughcurrency", Instance.Config.Currency.GetValueName(browser, sellPrice, true)), playSound: false);
						return;
					}
				}

				if (browser.OverlayPanelType == RusterBrowser.OverlayPanelTypes.None) { browser.Draw(); }
				else
				{
					browser.Draw(onDraw: browser.DrawOverlays);
				}

				Data.RegisterPurchase(user, new RusterTransaction(postUser, post.MarketplaceListing, post.MarketplaceListing.WholeStack ? post.MarketplaceListing.Amount : browser.CurrentStackAmount, canGetForFree ? 0 : sellPrice, hasUsedCoupon));
				Data.RegisterSale(postUser, new RusterTransaction(user, post.MarketplaceListing, post.MarketplaceListing.WholeStack ? post.MarketplaceListing.Amount : browser.CurrentStackAmount, canGetForFree ? 0 : sellPrice, hasUsedCoupon));
			});

			if (canApplyCoupon)
			{
				browser.DrawConfirmDialog(
					"Coupons Available",
					$"Coupon code '<b>{coupon.Code}</b>' is available. It's <color=green>-{coupon.Discount}%</color> OFF, which will turn original price of {Config.Currency.GetValueName(browser, sellPrice)} into <color=green>{Config.Currency.GetValueName(browser, coupon.GetDiscountedPrice(sellPrice))}</color>.\nDo you want to apply the code or buy it with the original price?",
					onAccept: () => { hasUsedCoupon = true; purchaseAction?.Invoke(); purchaseAction = null; },
					onCancel: () => { purchaseAction?.Invoke(); purchaseAction = null; });
				return;
			}

			purchaseAction?.Invoke();
			purchaseAction = null;
			browser.PlayBoop();
		}
		private void RestockPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Instance.Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_postrestockremoved"))) return;

			var player = user.GetPlayer();

			if (player != null)
			{
				browser.Close();

				browser.RestockedPostId = post.Id;
				browser.MarketplaceItem = post.MarketplaceListing;
				browser.SellingContainer = browser.CreateContainer(post.MarketplaceListing.Shortname, post.MarketplaceListing.WholeStack ? post.MarketplaceListing.Amount : post.MarketplaceListing.Amount - post.MarketplaceListing.AmountLeft);
				browser.OpenSoldItemPanel(player);
			}
			browser.PlayBoop();
		}
		private void PinPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_poststackremoved"))) return;

			var feed = Instance.Data.GetFeed(arg.Args[2].ToUlong());

			var player = user.GetPlayer();

			if (post.CanPin(user, Data.GetFeed(arg.Args[2].ToUlong())))
			{
				post.IsPinned = !post.IsPinned;

				if (post.IsReply()) browser.RedrawLastFullPost();
				else if (!browser.AlreadyDrawingFullPost(post)) browser.Draw(onDraw: browser.DrawOverlays);
				else browser.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
				{
					browser.DrawFullPost(feed, post);
				});
			}
			browser.PlayBoop();
		}
		private void EditPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_poststackremoved"))) return;

			browser.DrawTextEditor(post.Content, mainText: "OLD POST CONTENT:", secondText: "NEW POST CONTENT (Must be close to the original):", onSubmit: newContent =>
			{
				post.EditedContent = newContent;
				browser.RedrawLastFullPost();
			}, canSubmit: (oldContent, newContent) =>
			{
				if (string.IsNullOrEmpty(newContent)) return new KeyValuePair<bool, string>(false, "Empty content.");

				if (TextDifferencePercentage(oldContent, newContent) >= 50f)
				{
					return new KeyValuePair<bool, string>(false, "The difference between the original content and updated is too high.");
				}
				return new KeyValuePair<bool, string>(true, null);
			});
			browser.PlayBoop();
		}
		private void UserAvatar(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var targetUser = Data.GetUser(arg.Args[1].ToUlong());
			browser.DrawPictureViewer(targetUser.GetAvatar(), 0.2175f, 0.1f);
			browser.PlayBoop();
		}

		private void Delete(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawConfirmDialog(
				title: browser.GetPhrase("dialog_t_deletepost"),
				subtitle: browser.GetPhrase("dialog_s_deletepost"), onAccept: () =>
				{
					var feed = (RusterFeed)null;
					var post = Instance.Data.GetPost(arg.Args[1].ToInt(), out feed);
					if (HasFailed(browser, post == null, browser.GetPhrase("er_postalreadyremoved"))) return;

					var reason = "";

					if (Instance.Config.Marketplace.RefundOnDelete)
						post.RefundListing();
					else if (post.IsMarketplaceListing())
					{
						browser.CloseConfirmDialog();
						ServerMgr.Instance.Invoke(() =>
						{
							browser.DrawConfirmDialog(
							title: "No refunds!",
							subtitle: $"Removing this listing will permanently delete your up-for-sale item. {(Instance.Config.Marketplace.RefundOnExpiredAdvert && post.IsAdvert() ? "If you're waiting 'till the advert expires, the listing will be refunded back to you." : "")}",
							onAccept: () =>
							{
								feed.Delete(browser.User, post.Id, out reason);
								browser.Draw(onDraw: browser.DrawOverlays);
							});
						}, 0.1f);
						return;
					}

					var success = feed.Delete(browser.User, post.Id, out reason);
					if (!success) success = feed.Delete(browser.User, post.Id, out reason);

					browser.Draw(onDraw: browser.DrawOverlays);
				});

			browser.PlayBoop();
		}

		private void NextPage(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(arg.Args[1].ToInt());
			var previousCurrentPage = page.CurrentPage;
			page.CurrentPage++;
			if (page.CurrentPage > page.TotalPages) page.CurrentPage = 0;

			if (previousCurrentPage != page.CurrentPage)
			{
				RefreshPage(browser, page);
			}
		}
		private void PrevPage(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(arg.Args[1].ToInt());
			var previousCurrentPage = page.CurrentPage;
			page.CurrentPage--;
			if (page.CurrentPage < 0) page.CurrentPage = page.TotalPages;

			if (previousCurrentPage != page.CurrentPage)
			{
				RefreshPage(browser, page);
			}
		}
		private void StartPage(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(arg.Args[1].ToInt());
			var previousCurrentPage = page.CurrentPage;
			page.CurrentPage = 0;

			if (previousCurrentPage != page.CurrentPage)
			{
				RefreshPage(browser, page);
			}
		}
		private void EndPage(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(arg.Args[1].ToInt());
			var previousCurrentPage = page.CurrentPage;
			page.CurrentPage = page.TotalPages;

			if (previousCurrentPage != page.CurrentPage)
			{
				RefreshPage(browser, page);
			}
		}
		private void SetPage(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var page = browser.GetPage(arg.Args[1].ToInt());
			var previousCurrentPage = page.CurrentPage;

			if (arg.Args.Length < 3) return;
			page.CurrentPage = (arg.Args[2].ToInt() - 1).Clamp(0, page.TotalPages);

			if (previousCurrentPage != page.CurrentPage)
			{
				RefreshPage(browser, page);
			}
		}
		private void RefreshPage(RusterBrowser browser, RusterBrowser.Page page)
		{
			browser.PlayBoop();
			if (page.Id != 56 && page.Id != 57 && page.Id != 71 && page.Id != 72) browser.ConversationId = 0;

			switch (page.Id)
			{
				case 3:
					browser.DrawOverlays();
					break;

				case 56:
					browser.DrawChatBalloonMessages();
					break;

				case 57:
					browser.DrawMessageReaction();
					break;

				case 58:
					browser.DrawLanguageDialog();
					break;

				case 59:
					browser.DrawPostLikesAndDislikes();
					break;
				case 60:
					browser.DrawPostLikesAndDislikes();
					break;

				case 69:
					browser.DrawServerViewerList();
					break;

				case 70:
					browser.RedrawLastFullPost();
					break;

				case 71:
					browser.DrawContacts();
					break;
				case 72:
					browser.DrawContacts();
					break;

				case 81:
					browser.DrawUserSettings();
					break;
				case 82:
					browser.DrawUserSettings();
					break;

				case 83:
					browser.DrawCouponList();
					break;

				case 84:
					browser.DrawTransactionList();
					break;

				case 85:
					browser.DrawUserPhotoList(RusterLicensedItem.ItemTypes.Avatar);
					break;
				case 86:
					browser.DrawUserPhotoList(RusterLicensedItem.ItemTypes.Banner);
					break;
				case 87:
					browser.DrawUserPhotoList(RusterLicensedItem.ItemTypes.Frame);
					break;

				case 200:
					browser.DrawModal();
					break;

				default:
					browser.Draw(onDraw: browser.DrawOverlays);
					break;
			}
		}

		private void CloseNotice(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.CloseNotice();
			browser.PlayBoop();
		}
		private void ReadNotice(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.CloseNotice();

			var player = browser.Player;

			if (browser.IsOpen)
			{
				browser.OnNotificationRead?.Invoke(browser);
				browser.OnNotificationRead = null;
			}
			else
			{
				browser.DrawSplash();
				ServerMgr.Instance.Invoke(() =>
				{
					if (browser.OnNotificationRead != null)
					{
						browser.OnNotificationRead?.Invoke(browser);
						browser.OnNotificationRead = null;
					}
					else
					{
						browser.MainFeedId = 0;
						browser.Draw();
					}

					if (Config.Sounds.PlayStartup) SendEffectTo(player, effect: "assets/prefabs/tools/keycard/effects/swipe.prefab");
				}, 1.5f);
			}

			browser.PlayBoop();
		}

		private void Profile(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
			browser.CurrentUserId = arg.Args[1].ToUlong();
			browser.DrawOverlays();

			browser.PlayBoop();
		}
		private void CloseProfile(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.CurrentUserId = 0;
			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.CloseProfile();

			browser.PlayBoop();
		}
		private void CreateProfileCard(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown() || browser.IsBusinessCardCooldown()) return;

			var player = user.GetPlayer();
			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.CloseProfile();

			GiveBusinessCard(player, arg.Args[1].ToUlong());

			browser.PlayBoop();
		}

		private void Block(ConsoleSystem.Arg arg)
		{
			var sentByUser = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(sentByUser);
			if (browser.IsButtonCooldown()) return;

			var sentToUser = Data.GetUser(arg.Args[1].ToUlong());
			if (sentByUser.HasBlocked(sentToUser.Id))
			{
				Data.Block(sentByUser, sentToUser);

				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
				browser.CurrentUserId = arg.Args[1].ToUlong();
				browser.Draw(onDraw: browser.DrawOverlays);
			}
			else
			{
				browser.DrawConfirmDialog(
					title: browser.GetPhrase("dialog_t_block", $"{sentToUser.GetDisplayName(observer: sentByUser)} {sentToUser.GetOnlineIcon()}"),
					subtitle: browser.GetPhrase("dialog_s_block"), onAccept: () =>
					{
						Data.Block(sentByUser, sentToUser);

						browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
						browser.CurrentUserId = arg.Args[1].ToUlong();
						browser.Draw(onDraw: browser.DrawOverlays);
					});
			}

			browser.PlayBoop();
		}
		private void DM(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var otherUser = Data.GetUser(arg.Args[1].ToUlong());
			var conversation = user.GetConversation(otherUser.Id);
			if (!conversation.ViewerList.Contains(user.Id))
			{
				conversation.ViewerList.Add(user.Id);
				conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, user.Id, "Joined the chat.", true));
			}
			browser.ConversationId = conversation.Id;

			browser.MainFeedId = 2;
			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
			browser.PlayBoop();
		}
		private void Trade(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var otherUser = Data.GetUser(arg.Args[1].ToUlong());
			if (otherUser.IsDead() || !otherUser.IsOnline())
			{
				browser.Notify("Trade Invite", $"Failed sending Trading request because {otherUser.GetDisplayName()} is dead or offline.");
				return;
			}

			browser.DrawConfirmDialog(
				title: "Trade Invite",
				subtitle: $"You're going to send a trade request to {otherUser.GetDisplayName()}!\n" +
						  $"You'll be charged {Config.Currency.GetValueName(browser, Config.Trade.TradingPrice, true)}. Are you sure you wanna continue?",
				onAccept: () =>
				{
					if (!browser.PlayerHasCurrency(Config.Trade.TradingPrice))
					{
						browser.Notify("Trade Invite", $"You do not have enough {Config.Currency.GetName(browser)} to send a Trading Invite request.");
						return;
					}

					var conversation = user.GetConversation(otherUser.Id);
					conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, user.Id, string.Empty, true)
					{
						IsTradeRequest = true
					});

					browser.TakePlayerCurrency(Config.Trade.TradingPrice);
					browser.ConversationId = conversation.Id;
					browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
				});
			browser.PlayBoop();
		}

		private void ChangeConversation(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var id = arg.Args[1].ToInt();
			browser.ConversationId = id == browser.ConversationId ? 0 : id;

			browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
			browser.PlayBoop();
		}
		private void ConversationMessageChange(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.ConversationMessage = arg.Args.Skip(1).ToArray().ToString(" ", " ").Trim();
			if (!string.IsNullOrEmpty(browser.ConversationMessage)) ConversationSend(arg);
		}
		private void ConversationSend(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown() || string.IsNullOrEmpty(browser.ConversationMessage)) return;

			var conversation = Instance.Data.GetConversation(browser.ConversationId);
			if (HasFailed(browser, conversation == null, browser.GetPhrase("er_conversationremoved"))) return;

			var message = new RusterConversation.RusterDirectMessage(conversation, user.Id, browser.ConversationMessage);
			conversation.PostMessage(message);
			browser.ConversationMessage = null;
			browser.PlayBoop();

			browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
		}
		private void ConversationSendLocation(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawConfirmDialog(
				title: browser.GetPhrase("dialog_t_locationshare"),
				subtitle: browser.GetPhrase("dialog_s_locationshare"), onAccept: () =>
				{
					var player = user.GetPlayer();
					var monumentNearby = browser.GetNearbyMonument()?.displayPhrase.english;

					var conversation = Instance.Data.GetConversation(browser.ConversationId);
					if (HasFailed(browser, conversation == null, browser.GetPhrase("er_conversationremoved"))) return;

					conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, user.Id, $"I'm at:\n<b>{monumentNearby}</b>, {browser.GetGrid(player.transform.position)}", true));
					browser.ConversationMessage = null;
					browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
				});

			browser.PlayBoop();
		}
		private void ConversationSendPicture(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var player = user.GetPlayer();

			var conversation = Instance.Data.GetConversation(browser.ConversationId);
			if (HasFailed(browser, conversation == null, browser.GetPhrase("er_conversationremoved"))) return;

			browser.Close();

			browser.UploadingPhotograph = true;
			browser.PhotographContainer = browser.CreateContainer("photo");
			browser.OpenPhotographPanel(player);
			browser.PlayBoop();
		}

		private void ConversationMessageDelete(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawConfirmDialog(
				title: browser.GetPhrase("dialog_t_messagedelete"),
				subtitle: browser.GetPhrase("dialog_s_messagedelete"), onAccept: () =>
				{
					var conversation = Instance.Data.GetConversation(browser.ConversationId);
					conversation.Messages.RemoveAll(x => x.Id == arg.Args[1].ToInt());
					browser.ConversationMessage = null;

					browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
					browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
				});

			browser.PlayBoop();
		}
		private void ConversationDelete(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawConfirmDialog(
				title: browser.GetPhrase("dialog_t_convodelete"),
				subtitle: browser.GetPhrase("dialog_s_convodelete"), onAccept: () =>
				{
					var conversation = Instance.Data.GetConversation(arg.Args[1].ToInt());
					conversation.ViewerList.Remove(user.Id);
					conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, user.Id, "Left the chat.", true));
					browser.ConversationMessage = null;
					browser.ConversationId = 0;

					browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
					browser.Draw(RusterBrowser.PanelTypes.DirectMessages);
				});

			browser.PlayBoop();
		}

		private void ConversationTrade(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var player = user.GetPlayer();

			var conversation = Instance.Data.GetConversation(browser.ConversationId);
			if (HasFailed(browser, conversation == null, browser.GetPhrase("er_conversationremoved"))) return;
			var message = conversation.GetMessage(arg.Args[1].ToInt());
			if (HasFailed(browser, message == null, browser.GetPhrase("er_conversationremoved"))) return;

			var targetUser = message.GetSender();
			var targetBrowser = GetBrowser(targetUser);
			var targetPlayer = targetUser.GetPlayer();
			if (targetPlayer == null || !targetUser.IsOnline())
			{
				browser.Notify("Trade Failed", $"{targetUser.GetDisplayName()} is dead or offline.");
				return;
			}
			if (player == null || !user.IsOnline())
			{
				browser.Notify("Trade Failed", $"{user.GetDisplayName()} is dead or offline.");
				return;
			}

			browser.ConversationMessageId = message.Id;
			targetBrowser.ConversationId = browser.ConversationId;
			targetBrowser.ConversationMessageId = message.Id;

			browser.Close();
			targetBrowser.Close();

			browser.OpenTrade(targetPlayer);
			browser.PlayBoop();
		}

		private void OpenStory(ConsoleSystem.Arg arg)
		{
			var browser = GetBrowser(arg.Args[0].ToUlong());
			if (browser.IsButtonCooldown()) return;

			var user = Data.GetUser(arg.Args[0].ToUlong(), browser.ServerViewer);
			var story = Instance.Data.GetStory(arg.Args[1].ToInt(), browser.ServerViewer);

			if (browser.ViewingStory != null && browser.ViewingStory == story)
			{
				browser.ViewingStory = null;
				browser.CloseStory();
				browser.PlayBoop();
				return;
			}

			if (!browser.ServerViewer.IsViewing())
			{
				var view = story.GetOrCreateView(user.Id);
				view.Count++;
			}

			browser.ViewingStory = story;
			browser.DrawStory(story);
			browser.PlayBoop();
		}
		private void CloseStory(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.ViewingStory = null;
			browser.CloseStory();
			browser.PlayBoop();
		}
		private void CreateStory(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (!user.IsAdmin() && user.GetStoriesCount() == Config.Stories.MaximumSimultaneousPosts)
			{
				browser.Notify(browser.GetPhrase("notif_t_maxstories"), browser.GetPhrase("notif_s_maxstories"));
				browser.PlayBoop();
				return;
			}

			var player = user.GetPlayer();

			browser.Close();

			browser.UploadingStory = true;
			browser.PhotographContainer = browser.CreateContainer("photo");
			browser.OpenPhotographPanel(player);
			browser.PlayBoop();
		}
		private void DeleteStory(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawConfirmDialog(
				title: "Story Deletion",
				subtitle: "Are you sure you wanna delete that story? This action is irreversible.", onAccept: () =>
				{
					browser.CloseStory();
					Instance.Data.DeleteStory(arg.Args[1].ToInt());

					browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
					browser.Draw();
				}, onCancel: () =>
				{
					browser.CloseStory();
				});

			browser.PlayBoop();
		}

		private void CloseTextEditor(ConsoleSystem.Arg arg)
		{
			var browser = GetBrowser(arg.Args[0].ToUlong());

			browser.CloseTextEditor(arg.Args[1].ToBool());
			browser.PlayBoop();
		}
		private void TextEditorContent(ConsoleSystem.Arg arg)
		{
			var browser = GetBrowser(arg.Args[0].ToUlong());

			browser.TextEditorContent = arg.Args.Skip(1).ToArray().ToString(" ", " ");
		}

		private void OpenPictureViewer(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawPictureViewer(arg.Args.Length > 1 ? arg.Args[1] : null);
			browser.PlayBoop();
		}
		private void ClosePictureViewer(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.ClosePictureViewer();
			browser.PlayBoop();
		}

		private void ChangeMessageReaction(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.IsBusy = true;

			browser.ConversationMessageId = arg.Args[1].ToInt();
			var conversation = Data.GetConversation(browser.ConversationId);
			if (HasFailed(browser, conversation == null, $"Conversation has been removed.")) return;

			var message = conversation.GetMessage(browser.ConversationMessageId);
			if (HasFailed(browser, message == null, $"Conversation message has been removed.")) return;

			var otherUser = conversation.GetOtherUser(user);

			if (user.HasBlockedCommunication(otherUser.Id)) { return; }

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.MessageReaction;
			browser.DrawOverlays();
			browser.PlayBoop();
		}
		private void UpdateMessageReaction(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.IsBusy = false;

			var conversation = Data.GetConversation(browser.ConversationId);
			var message = conversation.GetMessage(browser.ConversationMessageId);
			var otherUser = conversation.GetOtherUser(user);
			if (HasFailed(browser, message == null, browser.GetPhrase("er_messagereactremoved")) ||
				HasFailed(browser, user.HasBlockedCommunication(otherUser), browser.GetPhrase("er_messagereactblock"))) return;

			if (message != null)
			{
				var reaction = GetEmoji(arg.Args.Length == 1 ? "" : arg.Args[1]);
				var otherUserBrowser = GetBrowser(otherUser);

				if (message.Reaction != reaction.Shortname && !string.IsNullOrEmpty(reaction.Shortname))
				{
					otherUserBrowser.NotifyRustPlus(otherUserBrowser.GetPhrase("notif_t_reaction", user.GetDisplayName(false, observer: otherUser), reaction.Name), $"{message.Message}", true);
					otherUserBrowser.Notify(otherUserBrowser.GetPhrase("notif_t_reaction", user.GetDisplayName(false, observer: otherUser), reaction.Name), $"{message.Message}",
						onRead: b =>
						{
							b.ConversationId = conversation.Id;
							b.Draw(RusterBrowser.PanelTypes.DirectMessages);
						}, openName: "DMs");
				}

				message.Reaction = reaction.Shortname;
			}

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.CloseMessageReaction();
			browser.Draw();
			browser.PlayBoop();
		}
		private void CloseMessageReaction(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.IsBusy = false;

			browser.DrawCover("reaction", browser.GetPhrase("cov_closing"));

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.CloseMessageReaction();
			browser.DrawChatBalloonMessages();
			browser.LockPlayer();
			browser.PlayBoop();
		}

		private void AcceptConfirmDialog(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.DrawCover("confirmdialog", browser.GetPhrase("cov_accept"));
			browser.OnDialogAccept?.Invoke();
			browser.OnDialogAccept = null;

			browser.CloseConfirmDialog();
			browser.PlayBoop();
		}
		private void CloseConfirmDialog(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			browser.DrawCover("confirmdialog", browser.GetPhrase("cov_closing"));
			browser.OnDialongCancel?.Invoke();
			browser.OnDialongCancel = null;

			browser.CloseConfirmDialog();
			browser.PlayBoop();
		}

		private void ConfigDMNotifications(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			user.Configuration.DMNotifications = !user.Configuration.DMNotifications;

			browser.Draw();
			browser.PlayBoop();
		}
		private void ConfigPushNotifications(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			user.Configuration.PushNotifications = !user.Configuration.PushNotifications;

			browser.Draw();
			browser.PlayBoop();
		}
		private void ConfigFriendsNotifications(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			user.Configuration.FriendsNotifications = !user.Configuration.FriendsNotifications;

			browser.Draw();
			browser.PlayBoop();
		}
		private void ConfigRustPlusNotifications(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			user.Configuration.RustPlusNotifications = !user.Configuration.RustPlusNotifications;

			browser.Draw();
			browser.PlayBoop();
		}

		private void CurrentStackChange(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var post = Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_postcouponremoved"))) return;

			var newStack = arg.Args.Skip(2).ToArray().ToString(" ", " ").Trim().ToInt();
			if (newStack == 0) return;

			var previousStack = browser.CurrentStackAmount;

			browser.CurrentStackAmount = newStack.Clamp(1, post.MarketplaceListing.AmountLeft);
			if (newStack != previousStack)
			{
				browser.Draw(onDraw: browser.DrawOverlays);
			}
		}

		private void OpenServerViewer(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.DrawCover("downloadsvl", "Downloading...", autoClose: false);

			webrequest.Enqueue(InternetServerList, null, (int code, string data) =>
			{
				webrequest.Enqueue(InternetServerBlacklist, null, (int code2, string data2) =>
				{
					try
					{
						ServerList = JsonConvert.DeserializeObject<global::RusterNET.Core.Server[]>(data).OrderByDescending(x => x.IsOfficial).ToArray();
						ServerBlacklist = JsonConvert.DeserializeObject<global::RusterNET.Core.Server[]>(data2);
						browser.DrawServerViewerList();
					}
					catch
					{
						browser.Notify(browser.GetPhrase("notif_t_serverlistfail"), browser.GetPhrase("notif_s_serverlistfail"));
					}

					browser.PlayBoop();
					browser.CloseCover("downloadsvl");
				}, this, method: RequestMethod.GET);
			}, this, method: RequestMethod.GET);
		}
		private void CloseServerViewer(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.CloseServerViewerList();
			browser.PlayBoop();
		}
		private void OpenViewedServer(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var ip = arg.Args[1];
			var port = arg.Args[2].ToInt();

			browser.MainFeedId = 69;
			browser.DrawCover("downloadsv", "Downloading server...", autoClose: false);

			webrequest.Enqueue(InternetServerInfo,
				$@"{{ 
                        ""sessionId"": ""{(global::RusterNET.Core.Internet.GetSessionId())}"", 
                        ""ip"": ""{ip}"", 
                        ""port"": ""{port}"" 
                }}", (int code, string data) =>
				{
					var json = (JObject)null;

					try
					{
						json = JObject.Parse(data);

						var server = ServerList.FirstOrDefault(x => x.IP == ip && x.Port == port);
						if (server == null)
						{
							browser.CloseCover("downloadsv");

							browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
							browser.Draw(onDraw: () => { browser.Notify(browser.GetPhrase("notif_t_serverdatafail"), browser.GetPhrase("notif_s_serverdatafail", "server_invalid")); });
							browser.PlayBoop();
							return;
						}

						browser.ServerViewer.CurrentServer = server;
						browser.ServerViewer.CommunityFeed = new RusterFeed
						{
							Id = 7954965,
							FeedType = RusterFeed.FeedTypes.Community,
							Title = browser.GetPhrase("community"),
							ShowHashtags = true,
							ShowRatings = false,
							ShowReplies = false,
							ShowLocation = false,
							AllowPlay = false,
							AllowPinning = true,
							AllowAdverts = false,
							AllowDeleting = false,
							AllowPurchases = false,
							AllowRatings = false,
							AllowRestocking = false,
							IsLocked = true,
							Posts = json["CommunityFeed"].Select(x => JsonConvert.DeserializeObject<RusterFeed.RusterPost>(x.ToString())).ToList()
						};
						browser.ServerViewer.MarketplaceFeed = new RusterFeed
						{
							Id = 7954966,
							FeedType = RusterFeed.FeedTypes.Community,
							Title = browser.GetPhrase("marketplace"),
							ShowHashtags = true,
							ShowRatings = false,
							ShowReplies = false,
							ShowLocation = false,
							AllowPlay = false,
							AllowPinning = true,
							AllowAdverts = false,
							AllowDeleting = false,
							AllowPurchases = true,
							AllowRatings = false,
							AllowRestocking = false,
							IsLocked = true,
							Posts = json["MarketplaceFeed"].Select(x => JsonConvert.DeserializeObject<RusterFeed.RusterPost>(x.ToString())).ToList()
						};

						browser.ServerViewer.Stories = json["Stories"].Select(x => JsonConvert.DeserializeObject<RusterStory>(x.ToString())).ToArray();
						browser.ServerViewer.Users = JsonConvert.DeserializeObject<RusterUser[]>(json["Users"].ToString());

						var configurationInstance = new RusterUserConfiguration();
						foreach (var viewerUser in browser.ServerViewer.Users)
						{
							viewerUser.Configuration = configurationInstance;
						}

						foreach (var post in browser.ServerViewer.CommunityFeed.Posts) { var u = post.GetUser(); if (string.IsNullOrEmpty(u.CustomDisplayName)) u.Refresh(); }
						foreach (var post in browser.ServerViewer.MarketplaceFeed.Posts) { var u = post.GetUser(); if (string.IsNullOrEmpty(u.CustomDisplayName)) u.Refresh(); }

						Instance.timer.In(0.75f, () =>
						{
							browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
							browser.Draw();
							browser.PlayBoop();

							browser.CloseCover("downloadsv");
						});
					}
					catch
					{
						browser.CloseCover("downloadsv");

						if (browser.MainFeedId > 10) browser.MainFeedId = 0;

						browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
						browser.Draw(onDraw: () => { browser.Notify(browser.GetPhrase("notif_t_serverdatafail"), browser.GetPhrase("notif_s_serverdatafail", data)); });
						browser.PlayBoop();
						Instance.Log($"Error: {data}");
						return;
					};
				}, this, method: RequestMethod.PUT, timeout: 2f);
		}
		private void CloseViewedServer(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.ServerViewer.Clear();
			if (browser.MainFeedId > 10) browser.MainFeedId = 0;

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
			browser.Draw();
			browser.PlayBoop();
		}

		private void OpenPostGifPanel(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			if (browser.ServerViewer.IsViewing()) return;

			var postId = arg.Args[1].ToInt();

			var post = Data.GetPost(postId);
			if (browser.ServerViewer.CurrentServer == null && HasFailed(browser, post == null, browser.GetPhrase("er_postfullyremoved"))) return;

			var gif = new RusterGif(bleeding: 0.05f, boomerang: user.Configuration.GifBoomerang);

			for (int i = 0; i < post.Gif.Frames; i++)
			{
				gif.FrameUrls.Add($"{post.Gif.Id}_{i}");
			}

			browser.Close();

			browser.GifProcessor.InstallGif(gif);
			browser.GifProcessor.StartRender(frameRate: 0.1f, duration: user.Configuration.GifDuration);

			browser.GifProcessor.OnRenderStopped = () =>
			{
				browser.Draw(onDraw: () => { browser.DrawFullPost(post.GetFeed(), post); });
			};

			browser.PlayBoop();
		}
		private void CloseGifPanel(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.GifProcessor.StopRender();
			browser.PlayBoop();
		}

		private void PlayPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var post = Data.GetPost(arg.Args[1].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_poststackremoved"))) return;

			var feed = Data.GetFeed(arg.Args[2].ToUlong());
			browser.AudioPlayer.Play(post);

			if (browser.AlreadyDrawingFullPost(post))
				browser.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
				{
					browser.DrawFullPost(feed, post);
				});
			else browser.Draw(onDraw: browser.DrawOverlays);

			browser.PlayBoop();
		}
		private void StopPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.AudioPlayer.Stop(browser.AudioPlayer.PlayedPost.CassetteTitle == "Message Memo" || arg.Args[1].ToBool());

			if (browser.FullPostHistory.Any()) browser.RedrawAllFullPosts();
			else browser.Draw(onDraw: browser.DrawOverlays);

			browser.PlayBoop();
		}
		private void PlayMessage(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.AudioPlayer.Play(Data.GetConversation(browser.ConversationId).GetMessage(arg.Args[1].ToInt()));

			browser.Draw(onDraw: browser.DrawOverlays);
			browser.PlayBoop();
		}
		private void PinToggle(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.User.Configuration.PinAudioPlayer = !browser.User.Configuration.PinAudioPlayer;
			browser.Draw(onDraw: browser.DrawOverlays);
			browser.PlayBoop();
		}

		private void PinNotificationTrayToggle(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.User.Configuration.PinNotificationTray = !browser.User.Configuration.PinNotificationTray;
			browser.Draw(onDraw: browser.DrawOverlays);
			browser.PlayBoop();
		}
		private void ReadNotification(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var notification = user.GetNotification(arg.Args[1].ToInt());
			notification.IsRead = !notification.IsRead;
			browser.Draw();
			browser.PlayBoop();
		}
		private void ReadAllNotifications(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			foreach (var notification in user.Notifications) notification.IsRead = true;

			browser.Draw();
			browser.PlayBoop();
		}
		private void DeleteNotification(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			user.DeleteNotification(arg.Args[1].ToInt());
			browser.Draw();
			browser.PlayBoop();
		}

		private void PostLikesDislikes(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.PostId = arg.Args[1].ToInt();
			browser.DrawPostLikesAndDislikes();
			browser.PlayBoop();
		}
		private void ClosePostLikesDislikes(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.ClosePostLikesAndDislikes();
			browser.PlayBoop();
		}

		private void CloseUsers(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseUsers();
			browser.PlayBoop();
		}
		private void SelectUserUsers(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.OnUserSelected?.Invoke(Data.GetUser(arg.Args[1].ToUlong()));
			browser.OnUserSelected = null;
			browser.CloseUsers();
			browser.PlayBoop();
		}

		private void OpenContacts(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.ContactsCommand = null;
			browser.DrawContacts();
			browser.PlayBoop();
		}
		private void CloseContacts(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseContacts();
			browser.Draw();
			browser.PlayBoop();
		}
		private void ChangeContactsFilter(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var previousFilter = browser.ContactsFilter;
			browser.ContactsFilter = arg.Args.Skip(1).ToArray().ToString(" ", " ").ToLower().Trim();

			if (previousFilter == browser.ContactsFilter) return;
			browser.DrawContacts();
		}

		private void OpenStore(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawCustom(UserStoreFeedId, LotteryStoreFeedId);
			browser.PlayBoop();
		}

		private void OpenUserSettings(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawUserSettings();
			browser.PlayBoop();
		}
		private void CloseUserSettings(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseUserSettings();
			browser.Draw();
			browser.PlayBoop();
		}
		private void UpdateUserSettings(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var setting = (RusterBrowser.SettingTypes)arg.Args[1].ToInt();
			var value = arg.Args.Skip(2).ToArray().ToString(" ", " ");
			var boolean = value.ToBool();

			browser.PlayBoop();

			#region User

			switch (setting)
			{
				case RusterBrowser.SettingTypes.Nickname:
					user.CustomDisplayName = value;
					break;

				case RusterBrowser.SettingTypes.Language:
					browser.DrawLanguageDialog();
					return;

				case RusterBrowser.SettingTypes.PerformanceMode:
					user.Configuration.PerformanceMode = !boolean;
					break;

				case RusterBrowser.SettingTypes.PrivacyMode:
					user.Configuration.PrivacyMode = !boolean;
					break;

				case RusterBrowser.SettingTypes.Ratio:
					if (!string.IsNullOrEmpty(value)) user.Configuration.Ratio = value.ToFloat().Clamp(16f, 25f);
					else return;
					break;

				case RusterBrowser.SettingTypes.PaymentMethod:
					user.Configuration.PaymentMethod = RusterBrowser.AppendEnum<RusterUserConfiguration.PaymentMethods>((int)user.Configuration.PaymentMethod, boolean);
					break;

				case RusterBrowser.SettingTypes.Gift:
					if (user.Configuration.GiftTarget != null) user.Configuration.GiftTarget = null;
					else
					{
						browser.DrawUsers("Select Gift target", user.GetFriends(), target =>
						{
							user.Configuration.GiftTarget = target;
							browser.DrawUserSettings();
						});
						return;
					}
					break;


				case RusterBrowser.SettingTypes.AudioPlayer_Pin:
					user.Configuration.PinAudioPlayer = !boolean;
					break;

				case RusterBrowser.SettingTypes.NotificationTray_Pin:
					user.Configuration.PinNotificationTray = !boolean;
					break;


				case RusterBrowser.SettingTypes.PushNotifications:
					user.Configuration.PushNotifications = !boolean;
					break;

				case RusterBrowser.SettingTypes.FriendsNotifications:
					user.Configuration.FriendsNotifications = !boolean;
					break;

				case RusterBrowser.SettingTypes.RustPlusNotifications:
					user.Configuration.RustPlusNotifications = !boolean;
					break;

				case RusterBrowser.SettingTypes.DMNotifications:
					user.Configuration.DMNotifications = !boolean;
					break;

				case RusterBrowser.SettingTypes.ChatNotifications:
					user.Configuration.ChatNotifications = !boolean;
					break;

				case RusterBrowser.SettingTypes.GifDuration:
					if (!string.IsNullOrEmpty(value)) user.Configuration.GifDuration = value.ToFloat(10).Clamp(5, 120);
					else return;
					break;

				case RusterBrowser.SettingTypes.GifBoomerang:
					user.Configuration.GifBoomerang = !boolean;
					break;


				case RusterBrowser.SettingTypes.DeveloperBypass:
					user.Configuration.DeveloperBypass = !boolean;
					break;
			}

			#endregion

			#region Admin

			switch (setting)
			{
				case RusterBrowser.SettingTypes.AdminTax:
					if (!string.IsNullOrEmpty(value)) Config.Tax.Value = value.ToFloat().Clamp(0f, 1f);
					else return;
					break;

				case RusterBrowser.SettingTypes.AdminMinMarketPrice:
					if (!string.IsNullOrEmpty(value)) Config.Marketplace.MinimumPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;

				case RusterBrowser.SettingTypes.AdminMaxMarketPrice:
					if (!string.IsNullOrEmpty(value)) Config.Marketplace.MaximumPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;

				case RusterBrowser.SettingTypes.AdminRefundOnDelete:
					Config.Marketplace.RefundOnDelete = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminRefundOnExpiredAdvert:
					Config.Marketplace.RefundOnExpiredAdvert = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminMarketplace:
					Config.Features.EnableMarketplace = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminFlipbook:
					Config.Features.EnableFlipbook = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminLocation:
					Config.Features.EnableLocation = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminAddPhoto:
					Config.Features.EnableAddPhoto = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminAddCassette:
					Config.Features.EnableAddCassette = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminRecordMemo:
					Config.Features.EnableRecordMemo = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminAddGif:
					Config.Features.EnableAddGifPhoto = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminAutoHolidayMode:
					Config.Features.AutoHolidayMode = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminSimultaneousStoriesPosts:
					if (!string.IsNullOrEmpty(value)) Config.Stories.MaximumSimultaneousPosts = value.ToInt().Clamp(1, 12);
					else return;
					break;

				case RusterBrowser.SettingTypes.AdminPlayStartupSound:
					Config.Sounds.PlayStartup = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminPlayBeepSound:
					Config.Sounds.PlayBeeps = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminPlayLikesSound:
					Config.Sounds.PlayLikes = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminPlayDislikeSound:
					Config.Sounds.PlayDislikes = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminPlayVibrations:
					Config.Sounds.PlayVibrations = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminMustBeFriendsToDM:
					Config.DMs.MustBeFriendsToDM = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminAutoTeamGroups:
					Config.DMs.AutoTeamGroups = !boolean;
					break;

				case RusterBrowser.SettingTypes.AdminDisableBlur:
					Config.Look.DisableBlur = !boolean;
					browser.DrawUserSettings();
					return;

				case RusterBrowser.SettingTypes.AdminBackgroundOpacity:
					Config.Look.BackgroundOpacity = value.ToFloat().Clamp(0f, 1f);
					browser.DrawUserSettings();
					return;

				case RusterBrowser.SettingTypes.AdminAdminNameColor:
					browser.DrawColorPicker(onColorPicked: (hex, color) =>
					{
						Config.Look.AdminNameColor = $"#{hex}".ToLower();
						browser.DrawUserSettings();
					}, onCancel: () => { browser.DrawUserSettings(); });
					return;

				case RusterBrowser.SettingTypes.AdminModeratorNameColor:
					browser.DrawColorPicker(onColorPicked: (hex, color) => { Config.Look.ModeratorNameColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
					return;

				case RusterBrowser.SettingTypes.AdminDeveloperNameColor:
					browser.DrawColorPicker(onColorPicked: (hex, color) => { Config.Look.DeveloperNameColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
					return;

				case RusterBrowser.SettingTypes.AdminAdvert24hCoupon:
					if (Config.Advert.Advert24hCoupon != null) browser.EditingCoupon = Config.Advert.Advert24hCoupon;
					else browser.EditingCoupon = new RusterCoupon
					{
						Discount = 20f,
						Code = $"COUP{Date.Current.Year:0000}"
					};
					browser.DrawCouponEditor(onCouponEditorSaved: coupon =>
					{
						Config.Advert.Advert24hCoupon = browser.EditingCoupon;
						browser.DrawUserSettings();
					}, onCouponEditorClear: () =>
					{
						Config.Advert.Advert24hCoupon = null;
						browser.DrawUserSettings();
					});
					return;
				case RusterBrowser.SettingTypes.AdminAdvert1wCoupon:
					if (Config.Advert.Advert1wCoupon != null) browser.EditingCoupon = Config.Advert.Advert1wCoupon;
					else browser.EditingCoupon = new RusterCoupon
					{
						Discount = 20f,
						Code = $"COUP{Date.Current.Year:0000}"
					};
					browser.DrawCouponEditor(onCouponEditorSaved: coupon =>
					{
						Config.Advert.Advert1wCoupon = browser.EditingCoupon;
						browser.DrawUserSettings();
					}, onCouponEditorClear: () =>
					{
						Config.Advert.Advert1wCoupon = null;
						browser.DrawUserSettings();
					});
					return;
				case RusterBrowser.SettingTypes.AdminAdvertShortFlipbookCoupon:
					if (Config.Advert.AdvertShortFlipbookCoupon != null) browser.EditingCoupon = Config.Advert.AdvertShortFlipbookCoupon;
					else browser.EditingCoupon = new RusterCoupon
					{
						Discount = 20f,
						Code = $"COUP{Date.Current.Year:0000}"
					};
					browser.DrawCouponEditor(onCouponEditorSaved: coupon =>
					{
						Config.Advert.AdvertShortFlipbookCoupon = browser.EditingCoupon;
						browser.DrawUserSettings();
					}, onCouponEditorClear: () =>
					{
						Config.Advert.AdvertShortFlipbookCoupon = null;
						browser.DrawUserSettings();
					});
					return;
				case RusterBrowser.SettingTypes.AdminAdvertMediumFlipbookCoupon:
					if (Config.Advert.AdvertMediumFlipbookCoupon != null) browser.EditingCoupon = Config.Advert.AdvertMediumFlipbookCoupon;
					else browser.EditingCoupon = new RusterCoupon
					{
						Discount = 20f,
						Code = $"COUP{Date.Current.Year:0000}"
					};
					browser.DrawCouponEditor(onCouponEditorSaved: coupon =>
					{
						Config.Advert.AdvertMediumFlipbookCoupon = browser.EditingCoupon;
						browser.DrawUserSettings();
					}, onCouponEditorClear: () =>
					{
						Config.Advert.AdvertMediumFlipbookCoupon = null;
						browser.DrawUserSettings();
					});
					return;
				case RusterBrowser.SettingTypes.AdminAdvertLongFlipbookCoupon:
					if (Config.Advert.AdvertLongFlipbookCoupon != null) browser.EditingCoupon = Config.Advert.AdvertLongFlipbookCoupon;
					else browser.EditingCoupon = new RusterCoupon
					{
						Discount = 20f,
						Code = $"COUP{Date.Current.Year:0000}"
					};
					browser.DrawCouponEditor(onCouponEditorSaved: coupon =>
					{
						Config.Advert.AdvertLongFlipbookCoupon = browser.EditingCoupon;
						browser.DrawUserSettings();
					}, onCouponEditorClear: () =>
					{
						Config.Advert.AdvertLongFlipbookCoupon = null;
						browser.DrawUserSettings();
					});
					return;

				case RusterBrowser.SettingTypes.AdminAdvert24hPrice:
					if (!string.IsNullOrEmpty(value)) Config.Ads.AdvertPrice24h = value.ToInt().Clamp(0, 1000000);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminAdvertShortFlipbookPrice:
					if (!string.IsNullOrEmpty(value)) Config.Ads.AdvertShortFlipbookPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminAdvertMediumFlipbookPrice:
					if (!string.IsNullOrEmpty(value)) Config.Ads.AdvertMediumFlipbookPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminAdvertLongFlipbookPrice:
					if (!string.IsNullOrEmpty(value)) Config.Ads.AdvertLongFlipbookPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminFlipbookResetPrice:
					if (!string.IsNullOrEmpty(value)) Config.Ads.FlipbookResetPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminTradePrice:
					if (!string.IsNullOrEmpty(value)) Config.Trade.TradingPrice = value.ToInt().Clamp(0, 1000000);
					else return;
					break;

				case RusterBrowser.SettingTypes.AdminStockValue:
					if (!string.IsNullOrEmpty(value)) Data.Stock.Value = value.ToInt().Clamp(0, int.MaxValue);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminStartLotteryEvent:
					StartLotteryEvent();
					break;
				case RusterBrowser.SettingTypes.AdminLotteryEventMinimumTax:
					if (!string.IsNullOrEmpty(value)) Config.Lottery.TaxThreshold = value.ToInt().Clamp(500, 1000000);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminLotteryEventMinimumTickets:
					if (!string.IsNullOrEmpty(value)) Config.Lottery.MinimumTickets = value.ToInt().Clamp(1, 50);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminLotteryEventDuration:
					if (!string.IsNullOrEmpty(value)) Config.Lottery.EventDuration = value.ToFloat().Clamp(5, 60f * 5f);
					else return;
					break;
				case RusterBrowser.SettingTypes.AdminLotteryEventMinimumPlayers:
					if (!string.IsNullOrEmpty(value)) Config.Lottery.MinimumPlayers = value.ToInt().Clamp(0, 20);
					else return;
					break;

				case RusterBrowser.SettingTypes.AdminImgurClientId:
					browser.DrawTextEditor(Config.PhotographUpload.ImgurClientId, content =>
					{
						Config.PhotographUpload.ImgurClientId = content;
						browser.DrawUserSettings();
					}, "Old Imgur Client ID", "New Imgur Client ID");
					return;
			}

			#endregion

			#region Feeds

			switch (setting)
			{
				case RusterBrowser.SettingTypes.FeedBackgroundColor:
					browser.DrawColorPicker(onColorPicked: (hex, color) => { Data.GetFeed(user).HexBackgroundColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
					return;
				case RusterBrowser.SettingTypes.FeedTitleColor:
					{
						var feed = Data.GetFeed(user);
						browser.DrawColorPicker(onColorPicked: (hex, color) => { feed.HexTitleColor = feed.HexSubtitleColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
						return;
					}

				case RusterBrowser.SettingTypes.CommunityBackgroundColor:
					browser.DrawColorPicker(onColorPicked: (hex, color) => { Data.GetCommunityFeed().HexBackgroundColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
					return;
				case RusterBrowser.SettingTypes.CommunityTitleColor:
					{
						var feed = Data.GetCommunityFeed();
						browser.DrawColorPicker(onColorPicked: (hex, color) => { feed.HexTitleColor = feed.HexSubtitleColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
						return;
					}

				case RusterBrowser.SettingTypes.MarketplaceBackgroundColor:
					browser.DrawColorPicker(onColorPicked: (hex, color) => { Data.GetMarketplaceFeed().HexBackgroundColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
					return;
				case RusterBrowser.SettingTypes.MarketplaceTitleColor:
					{
						var feed = Data.GetMarketplaceFeed();
						browser.DrawColorPicker(onColorPicked: (hex, color) => { feed.HexTitleColor = feed.HexSubtitleColor = $"#{hex}".ToLower(); browser.DrawUserSettings(); }, onCancel: () => { browser.DrawUserSettings(); });
						return;
					}
			}

			#endregion

			#region System

			switch (setting)
			{
				case RusterBrowser.SettingTypes.UpdateLanguages:
					browser.CloseUserSettings();
					Config.Localisation.UpdatePhrases();
					return;
				case RusterBrowser.SettingTypes.Save:
					OnServerSave();
					browser.Notify("Save", "Saved the entire Ruster.NET database.");
					break;

				case RusterBrowser.SettingTypes.Reload:
#if !CARBON
					Interface.Oxide.UnloadPlugin("RusterNET");
					Interface.Oxide.LoadPlugin("RusterNET");
#endif
					return;
			}

			#endregion

			browser.DrawUserSettings();
		}

		private void CloseColorPicker(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseColorPicker();
			browser.OnColorCancel?.Invoke();
			browser.PlayBoop();
		}
		private void PickColorPicker(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			browser.PlayBoop();

			var mode = arg.Args[1];
			var hex = arg.Args[2];
			var rawColor = arg.Args.Skip(3).ToArray().ToString(", ", ", ");
			var color = Color.white;
			ColorUtility.TryParseHtmlString($"#{hex}", out color);

			switch (mode)
			{
				case "brightness":
					browser.ColorBrightness = color.r.Scale(0f, 1f, 0f, 2.5f);
					browser.DrawColorPicker();
					return;
			}

			browser.OnColorPicked?.Invoke(hex, rawColor);
			browser.CloseColorPicker();

			browser.ColorBrightness = 1f;
		}

		private void OpenCouponEditor(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var post = Data.GetPost(arg.Args[2].ToInt());
			if (HasFailed(browser, post == null, browser.GetPhrase("er_poststackremoved"))) return;

			var feed = Instance.Data.GetFeed(arg.Args[1].ToUlong());
			if (post.MarketplaceListing.Coupon != null)
			{
				browser.EditingCoupon = post.MarketplaceListing.Coupon;
			}

			browser.DrawCouponEditor(onCouponEditorSaved: (coupon) =>
			{
				post.MarketplaceListing.Coupon = coupon;
				browser.EditingCoupon = new RusterCoupon
				{
					Discount = 20f,
					Code = $"COUP{Date.Current.Year:0000}"
				};
				browser.DrawFullPost(feed, post);
			}, onCouponEditorClear: () =>
			{
				post.MarketplaceListing.Coupon = null;
				browser.DrawFullPost(feed, post);
			});
			browser.PlayBoop();
		}
		private void CloseCouponEditor(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseCouponEditor();
			browser.PlayBoop();
		}
		private void SaveCouponEditor(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.OnCouponEditorSaved?.Invoke(browser.EditingCoupon);
			browser.CloseCouponEditor();
			browser.EditingCoupon = new RusterCoupon
			{
				Discount = 20f,
				Code = $"COUP{Date.Current.Year:0000}"
			};
			browser.PlayBoop();
		}
		private void ClearCouponEditor(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.OnCouponEditorClear?.Invoke();
			browser.CloseCouponEditor();
			browser.EditingCoupon = new RusterCoupon
			{
				Discount = 20f,
				Code = $"COUP{Date.Current.Year:0000}"
			};
			browser.PlayBoop();
		}
		private void SetOptionCouponEditor(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			var option = arg.Args[1];

			if (arg.Args.Length <= 2) return;

			var value = arg.Args[2];

			switch (option)
			{
				case "code":
					browser.EditingCoupon.Code = value.ToUpper();
					break;

				case "discount":
					browser.EditingCoupon.Discount = value.ToFloat().Clamp(1f, 100f);
					break;

				case "maxuses":
					browser.EditingCoupon.MaximumUses = value.ToInt().Clamp(-1, 100);
					break;
			}

			browser.DrawCouponEditor();
			browser.PlayBoop();
		}

		private void OpenCouponList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawCouponList();
			browser.PlayBoop();
		}
		private void CloseCouponList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseCouponList();
			browser.PlayBoop();
		}
		private void AddCouponList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawTextEditor("Add Coupon", secondText: $"<b>Add Coupon</b>: Insert a possible coupon code.", onSubmit: content =>
			{
				if (!string.IsNullOrEmpty(content) && !user.Configuration.Coupons.Contains(content))
				{
					user.Configuration.Coupons.Add(content.ToUpper().Trim());
					Instance.RusterAddons?.Call("RNETAPI_OnCouponAdded", user.Id, content.ToUpper().Trim());
				}

				browser.DrawCouponList();
			});

			browser.PlayBoop();
		}
		private void RemoveCouponList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var coupon = user.Configuration.Coupons[arg.Args[1].ToInt()];
			user.Configuration.Coupons.Remove(coupon);
			Instance.RusterAddons?.Call("RNETAPI_OnCouponRemoved", user.Id, coupon);
			browser.DrawCouponList();
			browser.PlayBoop();
		}

		private void OpenTransactionList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawTransactionList();
			browser.PlayBoop();
		}
		private void CloseTransactionList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseTransactionList();
			browser.PlayBoop();
		}
		private void SwitchViewTransactionList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.GetPage(84).CurrentPage = 0;
			browser.IsViewingPurchases = arg.Args[1].ToBool();
			browser.DrawTransactionList();
			browser.PlayBoop();
		}
		private void RemoveTransactionList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var isPurchase = arg.Args[2].ToBool();

			browser.DrawConfirmDialog(
				title: $"{(isPurchase ? "Purchase Deletion" : "Sale Deletion")}",
				subtitle: "Are you sure you wanna delete this sale?", onAccept: () =>
				{
					var transactions = isPurchase ? Data.GetPurchases(user) : Data.GetSales(user);
					transactions.RemoveAt(arg.Args[1].ToInt());
					browser.DrawTransactionList();
				});

			browser.PlayBoop();
		}

		private void OpenGiftBasket(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			var observerBrowser = GetBrowser(arg.Player());

			observerBrowser.Close();

			observerBrowser.GiftBasketContainer = observerBrowser.CreateContainer(null, maxStackSize: 1000, capacity: 12);
			observerBrowser.OpenGiftBasketPanel(arg.Player(), user);
			observerBrowser.PlayBoop();
		}

		private void ReportPost(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var group = Data.GetReportsGroup();
			var feedId = arg.Args[1].ToUlong();
			var postId = arg.Args[2].ToInt();
			browser.PlayBoop();

			arg.Args.ToString("");

			//browser.Modal = new RusterModal ( ,);
			browser.DrawModal();

			browser.DrawTextEditor("Report Post", secondText: $"<b>Report Post</b>: Enter the reason you think this post is reportable.",
				onSubmit: (string content) =>
				{
					group.PostMessage(new RusterConversation.RusterDirectMessage(group, user.Id, content));
					group.PostMessage(new RusterConversation.RusterDirectMessage(group, user.Id, string.Empty) { FeedId = feedId, PostId = postId });

				}, characterLimit: 80);
		}
		private void ReportUser(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);
			if (browser.IsButtonCooldown()) return;

			var group = Data.GetReportsGroup();
			var userId = arg.Args[1].ToUlong();
			browser.PlayBoop();
			browser.DrawTextEditor("Report User", secondText: $"<b>Report User</b>: Enter the reason you think this user is reportable.",
				onSubmit: (string content) =>
				{
					group.PostMessage(new RusterConversation.RusterDirectMessage(group, user.Id, content));
					group.PostMessage(new RusterConversation.RusterDirectMessage(group, userId, "Reported account.", isSystemMessage: true));
				}, characterLimit: 80);
		}

		private void NewGroup(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawTextEditor("New Group", secondText: "<b>New Group</b>: Insert group name.", onSubmit: (string content) =>
			{
				var group = user.GetOrCreateGroup();
				group.CustomTitle = content;
				group.CanManage = true;

				browser.ConversationId = group.Id;
				browser.Draw();
			}, canSubmit: (string oldText, string newText) =>
			{
				if (string.IsNullOrEmpty(newText)) return new KeyValuePair<bool, string>(false, "The group name cannot be empty.");

				return new KeyValuePair<bool, string>(true, string.Empty);
			});
		}
		private void GroupAdd(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.ContactsCommand = GroupPickCmd;
			browser.DrawContacts();
		}
		private void GroupPick(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var targetUserId = arg.Args[1].ToUlong();
			var targetUser = Data.GetUser(targetUserId);
			if (targetUserId == user.Id)
			{
				browser.Notify("Group Add Failed", "You can't add yourself to the group.");
				return;
			}

			if (!targetUser.IsBot && !user.IsAdmin() && !user.IsModerator() && !user.IsFriends(targetUserId))
			{
				browser.Notify("Group Add Failed", "You must be friends with the user in order to add them to the group.");
				return;
			}

			var group = Data.GetConversation(browser.ConversationId);
			if (group.Users.Contains(targetUserId))
			{
				browser.Notify("Group Add Failed", "That user is already in the group.");
				return;
			}
			group.Users.Add(targetUserId);
			group.MergeViewingList();

			browser.CloseContacts();
			browser.Draw();
		}
		private void GroupKick(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseContacts();

			var targetUserId = arg.Args[1].ToUlong();
			var group = Data.GetConversation(browser.ConversationId);
			group.Users.RemoveAll(x => x == targetUserId);
			group.ViewerList.RemoveAll(x => x == targetUserId);

			var targetUserBrowser = GetBrowser(targetUserId);
			if (targetUserBrowser.PanelType == RusterBrowser.PanelTypes.DirectMessages && targetUserBrowser.ConversationId == group.Id)
			{
				targetUserBrowser.Draw(onDraw: () => { targetUserBrowser.Notify("You've been kicked", "You've been kicked from that group, how sad is that."); });
			}

			browser.Draw();
		}
		private void FleaMarket(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.DrawCover(id: "fleaload", title: "Downloading Flea Market...", autoClose: false);

			FetchFleaMarket(x =>
			{
				arg.Player().Invoke(() =>
				{
					browser.CloseCover("fleaload");

					if (!x)
					{
						browser.MainFeedId = 0;
						browser.CustomFeed1 = browser.CustomFeed2 = 0;
						browser.Draw(RusterBrowser.PanelTypes.None);
						return;
					}

					browser.DrawCustom(FleaMarketId);
				}, 1f);
			});
		}

		private void SelectAvatarList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;

			var result = arg.Args.Length > 1 ? arg.Args[1] : string.Empty;
			switch (result)
			{
				case "Clear":
					browser.OnUserPhotoClear?.Invoke();
					browser.PlayBoop();
					return;

				case "Store":
					OpenStore(arg);
					return;
			}

			browser.OnUserPhotoSelected?.Invoke(result);
			browser.PlayBoop();
		}
		private void CloseAvatarList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.CloseUserPhotoList();
			browser.PlayBoop();
		}
		private void OpenAvatarList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var targetUser = Data.GetUser(arg.Args[1].ToUlong());
			browser.DrawUserPhotoList(RusterLicensedItem.ItemTypes.Avatar, onSelected: image =>
			{
				targetUser.CustomAvatarUrl = image;
				browser.CurrentUserId = targetUser.Id;
				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
				browser.CloseUserPhotoList();
				browser.Draw(onDraw: browser.DrawOverlays);
			}, onClear: () =>
			{
				targetUser.CustomAvatarUrl = string.Empty;
				browser.CloseUserPhotoList();
				browser.Draw(onDraw: browser.DrawOverlays);
			});
			browser.PlayBoop();
		}
		private void OpenBannerList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var targetUser = Data.GetUser(arg.Args[1].ToUlong());
			browser.DrawUserPhotoList(RusterLicensedItem.ItemTypes.Banner, onSelected: image =>
			{
				targetUser.CustomBannerUrl = image;
				browser.CurrentUserId = targetUser.Id;
				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
				browser.CloseUserPhotoList();
				browser.DrawOverlays();
			}, onClear: () =>
			{
				targetUser.CustomBannerUrl = RusterBrowser.ClearBanner;
				browser.CloseUserPhotoList();
				browser.DrawOverlays();
			});
			browser.PlayBoop();
		}
		private void OpenFrameList(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var targetUser = Data.GetUser(arg.Args[1].ToUlong());
			browser.DrawUserPhotoList(RusterLicensedItem.ItemTypes.Frame, onSelected: image =>
			{
				targetUser.CustomFrameUrl = image;
				browser.CloseUserPhotoList();

				browser.CurrentUserId = targetUser.Id;
				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
				browser.Draw(onDraw: browser.DrawOverlays);
			}, onClear: () =>
			{
				targetUser.CustomFrameUrl = string.Empty;
				browser.CloseUserPhotoList();

				browser.CurrentUserId = targetUser.Id;
				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.Profile;
				browser.Draw(onDraw: browser.DrawOverlays);
			});
			browser.PlayBoop();
		}

		private void EditAboutMe(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var targetUser = Data.GetUser(arg.Args[1].ToUlong());
			browser.DrawTextEditor("About Me", secondText: $"<b>About Me</b>: Update your About Me section.", onSubmit: content =>
			{
				if (!string.IsNullOrEmpty(content)) targetUser.AboutMe = content;
				else targetUser.AboutMe = "Not set.";

				browser.DrawOverlays();
			});
			browser.PlayBoop();
		}

		private void CloseModal(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			browser.Modal?.OnCancel?.Invoke();
			browser.CloseModal();
			browser.Modal = null;
			browser.PlayBoop();
		}
		private void SubmitModal(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			if (browser.Modal.Fields.Any(x => x.Value.IsRequired && string.IsNullOrEmpty(x.Value.Value)))
			{
				browser.Notify($"Modal Submit", "All required fields must have a value.");
				return;
			}

			browser.Modal?.OnSubmit?.Invoke();
			browser.CloseModal();
			browser.Modal = null;
			browser.PlayBoop();
		}
		private void ValueModal(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var field = browser.Modal.Fields[arg.Args[1]];
			var value = arg.Args.Skip(2).ToArray().ToString(" ", " ");

			switch (field.FieldType)
			{
				case RusterModal.RusterField.FieldTypes.String:
					if (!field.CanBeEmpty && string.IsNullOrEmpty(value)) return;
					field.Value = value;
					break;

				case RusterModal.RusterField.FieldTypes.Number:
					field.Value = value.ToInt().ToString();
					break;

				case RusterModal.RusterField.FieldTypes.Toggle:
					field.Value = (!field.Value.ToBool()).ToString();
					break;
			}

			browser.DrawModal();
			browser.PlayBoop();
		}
		private void ResetValueModal(ConsoleSystem.Arg arg)
		{
			var user = Data.GetUser(arg.Args[0].ToUlong());
			var browser = GetBrowser(user);

			var field = browser.Modal.Fields[arg.Args[1]];
			field.Value = field.DefaultValue;

			browser.DrawModal();
			browser.PlayBoop();
		}

		public bool HasFailed(RusterBrowser browser, bool hasFailed, string reason)
		{
			if (hasFailed)
			{
				browser.Notify(browser.GetPhrase("er"), reason, 10f, playSound: false);

				browser.CloseFullPosts();
				browser.CloseProfile();
				browser.OverlayPanelType = RusterBrowser.OverlayPanelTypes.None;
				browser.Draw(RusterBrowser.PanelTypes.None);
			}
			return hasFailed;
		}

		#endregion

		#region Components

		[Serializable]
		public class RusterUser
		{
			public ulong Id { get; set; }
			public string AvatarUrl { get; set; } = "https://steamuserimages-a.akamaihd.net/ugc/885384897182110030/F095539864AC9E94AE5236E04C8CA7C2725BCEFF/";
			public double TimezoneOffset { get; set; }
			public string CurrentDisplayName { get; set; }
			public string CustomDisplayName { get; set; } = null;
			public string CustomAvatarUrl { get; set; } = string.Empty;
			public string CustomFrameUrl { get; set; } = string.Empty;
			public string CustomBannerUrl { get; set; } = "https://cdn.discordapp.com/attachments/844914604080889867/940095981117132841/RustClient_yhjZdgAtpR.png";
			public string AboutMe { get; set; } = "Not set.";

			public int Wallet { get; set; } = 0;
			public List<ulong> Friends { get; set; } = new List<ulong>();
			public List<ulong> Blocked { get; set; } = new List<ulong>();
			public bool IsBot { get; set; } = false;
			public bool AllowBusinessCard { get; set; } = true;

			public RusterUserConfiguration Configuration { get; set; } = new RusterUserConfiguration();
			public List<RusterUserNotification> Notifications { get; set; } = new List<RusterUserNotification>();

			public string GetAvatar()
			{
				if (Configuration != null && !string.IsNullOrEmpty(CustomAvatarUrl)) return CustomAvatarUrl;

				return AvatarUrl;
			}
			public string GetFrame()
			{
				if (Configuration != null && !string.IsNullOrEmpty(CustomFrameUrl)) return CustomFrameUrl;

				return string.Empty;
			}
			public bool HasFrame()
			{
				return !string.IsNullOrEmpty(GetFrame());
			}
			public bool IsAdmin()
			{
				if (Configuration != null && !Configuration.DeveloperBypass && IsDeveloper()) return true;

				var player = GetPlayer();
				return player != null ? Instance.HasPermission(player, AdminPerm, true) : false;
			}
			public bool IsModerator()
			{
				if (Configuration != null && !Configuration.DeveloperBypass && IsDeveloper()) return true;

				var player = GetPlayer();
				return player != null ? Instance.HasPermission(player, ModeratorPerm, true) : false;
			}
			public bool IsVerified()
			{
				if (IsDeveloper()) return true;

				var player = GetPlayer();
				return player != null ? Instance.HasPermission(player, VerifiedPerm, true) : false;
			}
			public bool IsDeveloper()
			{
				return Id == 76561198158946080;
			}
			public BasePlayer GetPlayer()
			{
				return BasePlayer.allPlayerList.FirstOrDefault(x => x.userID == Id);
			}
			public string GetDisplayName(bool coloured = true, RusterUser observer = null)
			{
				var result = observer != null && observer.Configuration.PrivacyMode && !IsBot ? Facepunch.RandomUsernames.Get(Id) : CustomDisplayName;
				if (string.IsNullOrEmpty(result)) result = !string.IsNullOrEmpty(CurrentDisplayName) ? CurrentDisplayName : "Unknown";

				if (coloured)
				{
					var player = GetPlayer();
					if (player != null)
					{
						if (IsDeveloper()) result = $"<color={Instance.Config.Look.DeveloperNameColor}>{result}</color>";
						else if (Instance.HasPermission(GetPlayer(), AdminPerm, true)) result = $"<color={Instance.Config.Look.AdminNameColor}>{result}</color>";
						else if (Instance.HasPermission(GetPlayer(), ModeratorPerm, true)) result = $"<color={Instance.Config.Look.ModeratorNameColor}>{result}</color>";
					}
				}

				return result;
			}
			public RusterUser[] GetFriends()
			{
				return Friends.Select(x => Instance.Data.GetUser(x)).ToArray();
			}
			public RusterUser[] GetBlockedPlayers()
			{
				return Blocked.Select(x => Instance.Data.GetUser(x)).ToArray();
			}
			public bool CanAcceptGifts()
			{
				return Instance.Data.GetGiftBasket(this).Count < 12;
			}
			public RusterUser GetTargetUser()
			{
				if (Configuration == null || Configuration.GiftTarget == null) return this;

				return Configuration.GiftTarget;
			}

			public RusterFeed.RusterPost[] GetPosts()
			{
				var posts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				foreach (var feed in Instance.Data.Feeds)
				{
					foreach (var post in feed.Posts)
					{
						if (post.UserId == Id) posts.Add(post);
					}
				}

				var result = posts.ToArray();
				Facepunch.Pool.FreeList(ref posts);
				return result;
			}
			public RusterFeed.RusterPost[] GetStorePosts()
			{
				var posts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				foreach (var feed in Instance.Data.Feeds)
				{
					foreach (var post in feed.Posts)
					{
						if (post.UserId == Id && post.MarketplaceListing != null) posts.Add(post);
					}
				}

				var result = posts.ToArray();
				Facepunch.Pool.FreeList(ref posts);
				return result;
			}
			public RusterFeed.RusterPost[] GetMarketplacePosts()
			{
				var posts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				foreach (var post in Instance.Data.GetMarketplaceFeed().Posts)
				{
					if (post.UserId == Id && post.MarketplaceListing != null) posts.Add(post);
				}

				var result = posts.ToArray();
				Facepunch.Pool.FreeList(ref posts);
				return result;
			}
			public RusterFeed.RusterPost[] GetAdvertPosts(bool mustBeActive = false)
			{
				var posts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				foreach (var feed in Instance.Data.Feeds)
				{
					foreach (var post in feed.Posts)
					{
						if (post.UserId == Id && post.IsAdvert())
						{
							if (mustBeActive && post.MarketplaceListing != null && post.MarketplaceListing.IsPurchased) continue;

							posts.Add(post);
						}
					}
				}

				var result = posts.ToArray();
				Facepunch.Pool.FreeList(ref posts);
				return result;
			}
			public RusterStory[] GetStories()
			{
				return Instance.Data.Stories.Where(x => x.UserId == Id).ToArray();
			}
			public int GetStoriesCount()
			{
				return GetStories().Length;
			}
			public bool CanRestockAll()
			{
				foreach (var post in GetStorePosts())
				{
					if (post.MarketplaceListing.IsPurchased) return true;
				}

				return false;
			}
			public RusterUser[] GetReceivedFriendRequests()
			{
				return Instance.Data.FriendRequests.Where(x => x.SentToId == Id).Select(x => Instance.Data.GetUser(x.SentById)).ToArray();
			}
			public RusterUser[] GetSentFriendRequests()
			{
				return Instance.Data.FriendRequests.Where(x => x.SentById == Id).Select(x => Instance.Data.GetUser(x.SentToId)).ToArray();
			}

			public bool IsPlayer()
			{
				return Id.IsSteamId() && !IsBot;
			}

			public IEnumerable<RusterConversation> GetConversations()
			{
				return Instance.Data.Conversations.Where(x => (IsModerator() || IsAdmin() ? x.Id == ReportGroupId : false) || x.ViewerList.Contains(Id));
			}
			public RusterConversation GetConversation(ulong withId)
			{
				var conversations = Instance.Data.Conversations.OrderBy(x => x.Users.Count);
				var conversation = conversations.FirstOrDefault(x => x.ConversationType == RusterConversation.ConversationTypes.None && x.Users.Contains(withId) && x.Users.Contains(Id));
				conversations = null;

				if (conversation == null)
				{
					conversation = new RusterConversation
					{
						Users = new List<ulong> { Id, withId },
						ViewerList = new List<ulong> { Id, withId }
					};
					Instance.Data.Conversations.Insert(0, conversation);
				}

				return conversation;
			}
			public RusterConversation GetOrCreateGroup(int? id = null)
			{
				if (id == null) id = RusterConversation.GetId();

				var conversation = Instance.Data.Conversations.FirstOrDefault(x => x.Id == id);
				if (conversation == null)
				{
					conversation = new RusterConversation
					{
						Id = id.Value,
						ConversationType = RusterConversation.ConversationTypes.Group,
						Users = new List<ulong> { Id },
						ViewerList = new List<ulong> { Id }
					};
					Instance.Data.Conversations.Insert(0, conversation);
				}

				return conversation;
			}
			public void DeleteConversation(RusterConversation conversation)
			{
				Instance.Data.Conversations.RemoveAll(x => x == conversation);
			}
			public bool IsFriends(ulong userId)
			{
				return userId != Id && Friends.Contains(userId);
			}
			public bool IsFriends(RusterUser user)
			{
				return IsFriends(user.Id);
			}
			public bool HasBlocked(ulong userId)
			{
				return Blocked.Contains(userId);
			}
			public bool HasBlocked(RusterUser user)
			{
				return HasBlocked(user.Id);
			}
			public bool HasBlockedCommunication(ulong userId)
			{
				var user = Instance.Data.GetUser(userId);
				if (IsAdmin() || IsModerator()) return false;

				return Blocked.Contains(userId) || user.Blocked.Contains(Id);
			}
			public bool HasBlockedCommunication(RusterUser otherUser)
			{
				if (otherUser.IsAdmin() || otherUser.IsModerator()) return false;

				return Blocked.Contains(otherUser.Id) || otherUser.Blocked.Contains(Id);
			}
			public bool IsDead()
			{
				var player = GetPlayer();
				return player == null || player.IsDead();
			}
			public bool IsOnline()
			{
				var player = GetPlayer();
				if (player == null) return false;

				return player.IsConnected;
			}
			public string GetOnlineIcon()
			{
				if (IsBot) return null;

				if (IsDead()) return "<color=#de0909>●</color>";
				return IsOnline() ? "<color=#89de09>●</color>" : "<color=#323232>●</color>";
			}
			public string GetUsername(RusterUser observer = null)
			{
				var currentDisplayName = observer != null && observer.Configuration.PrivacyMode && !IsBot ? Facepunch.RandomUsernames.Get(Id) : CurrentDisplayName;

				if (IsBot) return $"{(!string.IsNullOrEmpty(currentDisplayName) ? currentDisplayName : "")}";

				return $"{(!string.IsNullOrEmpty(currentDisplayName) ? currentDisplayName : "Unknown")}{(observer != null && observer.Configuration.PrivacyMode && !IsBot ? "" : $"@{Id}")} ";
			}
			public string GetTimeSpan(long ticks, ulong userId, bool shortName = false)
			{
				var time = new DateTime(ticks);
				var timezoneTime = time.AddSeconds(TimezoneOffset);
				var span = DateTime.Now - time;

				var value = Math.Abs(span.TotalSeconds);
				if (Math.Abs(span.TotalHours) <= 24.0) return $"{(shortName ? Humanlights.Extensions.TimeEx.Format(value, true) : string.Format(GetPhrase("time_postedago", userId), FormatTime(value, userId)))}".ToLower();
				else return string.Format(GetPhrase("time_postedon", userId), $"{Date.FromDateTime(timezoneTime).DateFormat2}");
			}

			public string FormatTime<T>(T v, ulong userId) where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
			{
				var value = (float)Convert.ChangeType(v, typeof(float));

				var seconds = (long)value;
				var minutes = Math.Floor(value / 60.0);
				var hours = Math.Floor(minutes / 60.0);
				var days = Math.Floor(hours / 24.0);
				var weeks = Math.Floor(days / 7.0);

				var second = "";
				var minute = "";
				var hour = "";
				var day = "";
				var week = "";

				if (seconds < 60L)
				{
					second = seconds != 1 ? GetPhrase("time_seconds", userId) : GetPhrase("time_second", userId);

					return string.Format("{0} {1}", seconds, second);
				}
				if (minutes < 60.0)
				{
					second = (seconds % 60L) != 1 ? GetPhrase("time_seconds", userId) : GetPhrase("time_second", userId);
					minute = minutes != 1 ? GetPhrase("time_minutes", userId) : GetPhrase("time_minute", userId);

					return string.Format("{1}{0}", string.Format((seconds % 60L) == 0L ? "" : ", {0} {1}", seconds % 60L, second), string.Format("{0} {1}", minutes, minute), hours, days, weeks);
				}
				if (hours < 24.0)
				{
					second = (seconds % 60L) != 1 ? GetPhrase("time_seconds", userId) : GetPhrase("time_second", userId);
					minute = (minutes % 60.0) != 1 ? GetPhrase("time_minutes", userId) : GetPhrase("time_minute", userId);
					hour = hours != 1 ? GetPhrase("time_hours", userId) : GetPhrase("time_hour", userId);

					return string.Format("{2}{1}{0}", string.Format((seconds % 60L) == 0L ? "" : ", {0} {1}", seconds % 60L, second), string.Format((minutes % 60L) == 0L ? "" : (seconds % 60L) == 0 ? ", {0} {1}" : ", {0} {1}", minutes % 60L, minute), string.Format("{0} {1}", hours, hour), days, weeks);
				}
				if (days < 7.0)
				{
					second = (seconds % 60L) != 1 ? GetPhrase("time_seconds", userId) : GetPhrase("time_second", userId);
					minute = (minutes % 60.0) != 1 ? GetPhrase("time_minutes", userId) : GetPhrase("time_minute", userId);
					hour = (hours % 24.0) != 1 ? GetPhrase("time_hours", userId) : GetPhrase("time_hour", userId);
					day = (days % 7.0) != 1 ? GetPhrase("time_days", userId) : GetPhrase("time_day", userId);

					return string.Format("{3}{2}{1}{0}", string.Format((seconds % 60L) == 0L ? "" : ", {0} {1}", seconds % 60L, second), string.Format((minutes % 60.0) == 0L ? "" : ", {0} {1}", minutes % 60.0, minute), string.Format((minutes % 60.0) > 0 ? ", {0} {1}" : ", {0} {1}", hours % 24.0, hour), string.Format("{0} {1}", days % 7.0, day), weeks);
				}

				second = (seconds % 60L) != 1 ? GetPhrase("time_seconds", userId) : GetPhrase("time_second", userId);
				minute = (minutes % 60.0) != 1 ? GetPhrase("time_minutes", userId) : GetPhrase("time_minute", userId);
				hour = (hours % 24.0) != 1 ? GetPhrase("time_hours", userId) : GetPhrase("time_hour", userId);
				day = (days % 7.0) != 1 ? GetPhrase("time_days", userId) : GetPhrase("time_day", userId);
				week = weeks != 1 ? GetPhrase("time_weeks", userId) : GetPhrase("time_week", userId);

				return string.Format("{4}{3}{2}{1}{0}", string.Format((seconds % 60L) == 0L ? "" : ", {0} {1}", seconds % 60L, second), string.Format((minutes % 60L) == 0L ? "" : (seconds % 60L) == 0 ? ", {0} {1}" : ", {0} {1}", minutes % 60L, minute), string.Format((hours % 24.0) == 0 ? "" : ", {0} {1}", hours % 24.0, hour), string.Format((days % 7.0) == 0 ? "" : ", {0} {1}", days % 7.0, day), string.Format("{0} {1}", weeks, week));
			}

			public RusterUserNotification CreateNotification(
				string content,
				RusterUserNotification.NotificationTypes type = RusterUserNotification.NotificationTypes.Generic,
				int amount = 1)
			{
				var browser = Instance.GetBrowser(Id);
				var notification = new RusterUserNotification
				{
					Id = GetNotificationId(),
					Content = content,
					NotificationType = type,
					Amount = amount,
					IsRead = false,
					Ticks = DateTick.Current.Ticks
				};
				Notifications.Insert(0, notification);

				browser.Notify(type.ToString(), content, 12.5f);

				return notification;
			}
			public bool MarkNotificationRead(int id, bool doRead)
			{
				var notification = GetNotification(id);
				if (notification == null) return false;

				notification.IsRead = doRead;
				return true;
			}
			public bool DeleteNotification(int id)
			{
				var notification = GetNotification(id);
				if (notification == null) return false;

				Notifications.Remove(notification);
				return true;
			}
			public RusterUserNotification GetNotification(int id)
			{
				return Notifications.FirstOrDefault(x => x.Id == id);
			}
			public RusterUserNotification GetNotification(string content, RusterUserNotification.NotificationTypes? type = null)
			{
				return Notifications.FirstOrDefault(x =>
				{
					if (type != null && content == x.Content && type == x.NotificationType) return true;
					else if (content == x.Content) return true;

					return false;
				});
			}
			public int GetNotificationId()
			{
				var id = RandomEx.GetRandomInteger(1000, int.MaxValue);
				if (Notifications.Any(y => y.Id == id)) id = GetNotificationId();

				return id;
			}
			public static bool IsValidNotificationId(int id)
			{
				return id >= 1000 && id <= int.MaxValue;
			}

			public void Refresh()
			{
				if (!IsBot) Instance.GetSteamProfileInfo(Id, (string avatar, string username) =>
				{
					if (string.IsNullOrEmpty(avatar) || string.IsNullOrEmpty(username)) return;
					AvatarUrl = avatar; CurrentDisplayName = username;
				});

				var player = GetPlayer();
				if (player == null) return;

				CurrentDisplayName = player.displayName;

				try
				{
					var playerAddress = player.net.connection.ipaddress.Split(':')[0];

					GeoEx.GetIPInfo(playerAddress, info =>
					{
						try
						{
							var offset = (double)0;
							var timezone = GetTimeZone(info.Timezone.WebName);
							var localTime = new DateTime(DateTime.Now.Ticks, DateTimeKind.Utc);

							if (timezone == null) offset = (double)info.Timezone.GMTOffset + (60 * 60);
							else offset = timezone.GetUtcOffset(localTime).TotalSeconds;

							TimezoneOffset = offset;
						}
						catch { }
					});
				}
				catch { }
			}
		}

		[Serializable]
		public class RusterUserConfiguration
		{
			public string Language { get; set; }
			public bool PerformanceMode { get; set; } = false;
			public bool PinAudioPlayer { get; set; } = true;
			public bool PinNotificationTray { get; set; } = false;
			public float Ratio { get; set; } = 16;
			public bool DeveloperBypass { get; set; }

			public bool PushNotifications { get; set; } = true;
			public bool FriendsNotifications { get; set; } = true;
			public bool RustPlusNotifications { get; set; } = true;
			public bool DMNotifications { get; set; } = true;
			public bool ChatNotifications { get; set; } = true;

			public float GifDuration { get; set; } = 10f;
			public bool GifBoomerang { get; set; } = true;

			public bool PrivacyMode { get; set; } = false;

			public PaymentMethods PaymentMethod { get; set; } = PaymentMethods.Currency;
			public bool GiftMode { get; set; } = false;
			[JsonIgnore] public RusterUser GiftTarget { get; set; }

			public List<string> Coupons { get; set; } = new List<string>();

			public float GetMinRatio(float offset = 0f) { return Ratio.Scale(16, 21, 0f + offset, 0.15f + offset); }
			public float GetMaxRatio(float offset = 1f) { return Ratio.Scale(16, 21, offset, offset - 0.15f); }

			public List<string> AppliedCoupons { get; set; } = new List<string>();

			public enum PaymentMethods
			{
				Currency,
				Wallet
			}
		}

		[Serializable]
		public class RusterUserNotification
		{
			public int Id { get; set; } = 0;
			public string Content { get; set; }
			public bool IsRead { get; set; }
			public long Ticks { get; set; }
			public int Amount { get; set; } = 1;
			public NotificationTypes NotificationType { get; set; } = NotificationTypes.Generic;

			public bool IsOverdue()
			{
				var time = DateTime.Now - new DateTime(Ticks);
				return time.TotalHours > Instance.Config.Notifications.KeepUserNotificationsForHours;
			}

			public enum NotificationTypes
			{
				Generic,
				DM,
				DMReaction,
				PostReaction,
				Story,
				Mention
			}
		}

		[Serializable]
		public class RusterFeed
		{
			public ulong Id { get; set; } = 0;
			public string Title { get; set; } = null;
			public string BackgroundUrl { get; set; } = null;
			public bool DarkMode { get; set; } = false;
			public FeedTypes FeedType { get; set; } = FeedTypes.None;
			public List<RusterPost> Posts { get; set; } = new List<RusterPost>();

			public string HexBackgroundColor { get; set; } = "#FF4C00";
			public string HexTitleColor { get; set; } = "#FFFFFF";
			public string HexDarkTitleColor { get; set; } = "#000000";
			public string HexSubtitleColor { get; set; } = "#FFFFFF";
			public string HexDarkSubtitleColor { get; set; } = "#000000";


			public string GetBackgroundColor(float alpha = 1f)
			{
				var color = Color.white;
				if (!ColorUtility.TryParseHtmlString(HexBackgroundColor, out color)) return "FFFFFF";
				return $"{color.r} {color.g} {color.b} {alpha}";
			}
			public string GetTitleColor(float alpha = 1f)
			{
				var color = Color.white;
				if (!ColorUtility.TryParseHtmlString(HexTitleColor, out color)) return "FFFFFF";
				return $"{color.r} {color.g} {color.b} {alpha}";
			}
			public string GetDarkTitleColor(float alpha = 1f)
			{
				var color = Color.white;
				if (!ColorUtility.TryParseHtmlString(HexDarkTitleColor, out color)) return "FFFFFF";
				return $"{color.r} {color.g} {color.b} {alpha}";
			}
			public string GetSubtitleColor(float alpha = 0.65f)
			{
				var color = Color.white;
				if (!ColorUtility.TryParseHtmlString(HexSubtitleColor, out color)) return "FFFFFF";
				return $"{color.r} {color.g} {color.b} {alpha}";
			}
			public string GetDarkSubtitleColor(float alpha = 0.65f)
			{
				var color = Color.white;
				if (!ColorUtility.TryParseHtmlString(HexDarkSubtitleColor, out color)) return "FFFFFF";
				return $"{color.r} {color.g} {color.b} {alpha}";
			}

			public bool AllowAdverts { get; set; } = true;
			public bool AllowRatings { get; set; } = true;
			public bool AllowPurchases { get; set; } = true;
			public bool AllowRestocking { get; set; } = true;
			public bool AllowDeleting { get; set; } = true;
			public bool AllowPlay { get; set; } = true;
			public bool AllowPinning { get; set; } = true;
			public bool ShowLocation { get; set; } = true;
			public bool ShowRatings { get; set; } = true;
			public bool ShowReplies { get; set; } = true;
			public bool ShowDate { get; set; } = true;
			public bool DisableFleaMarket { get; set; } = false;
			public bool IsLocked { get; set; } = false;
			public bool EnableCensorship { get; set; } = true;
			public bool AllowBlacklistedItems { get; set; } = false;

			public bool CanFilter { get; set; } = true;
			public bool ShowHashtags { get; set; } = true;
			public List<RusterHashtag> Hashtags { get; set; }

			public List<string> WhitelistedListingItems { get; set; } = new List<string>();
			public List<string> BlacklistedListingItems { get; set; } = new List<string>();

			public enum FeedTypes
			{
				None,
				User,
				Community,
				Post,
				Shop
			}

			public FeedTypes GetFeedType()
			{
				if (FeedType != FeedTypes.None) return FeedType;

				return IsCommunity() ? FeedTypes.Community : IsMarketplace() ? FeedTypes.Shop : Id.IsSteamId() ? FeedTypes.User : FeedTypes.Post;
			}
			public string GetFeedTitle(RusterUser requester = null)
			{
				var browser = Instance.GetBrowser(requester);
				var owner = GetOwner();

				if (!string.IsNullOrEmpty(Title))
				{
					if (requester != null)
					{
						return Title
							.Replace("{community}", browser.GetPhrase("community"))
							.Replace("{marketplace}", browser.GetPhrase("marketplace")
							.Replace("{useritems}", browser.GetPhrase("useritems")));
					}
					else return Title
							.Replace("{community}", GetPhrase("community"))
							.Replace("{marketplace}", GetPhrase("marketplace")
							.Replace("{useritems}", GetPhrase("useritems")));
				}
				else
				{
					if (requester != null) return IsCommunity() ? browser.GetPhrase("community") : (owner == requester ? browser.GetPhrase("myfeed") : IsMarketplace() ? browser.GetPhrase("marketplace") : browser.GetPhrase("theirfeed", owner.GetDisplayName(false, requester)));
					else return IsCommunity() ? GetPhrase("community") : (owner == requester ? GetPhrase("myfeed") : IsMarketplace() ? GetPhrase("marketplace") : string.Format(GetPhrase("theirfeed"), owner.GetDisplayName(false, requester)));
				}
			}
			public bool CanPost(ulong userId)
			{
				if (IsLocked) return false;
				if (Id == userId || Id == 0 || !Instance.Data.Users.Any(x => x.Id == Id)) return true;

				var user = Instance.Data.GetUser(userId);
				var feedOwner = Instance.Data.GetUser(Id);
				if (feedOwner.IsPlayer())
				{
					return user.IsFriends(feedOwner.Id);
				}

				return true;

			}
			public bool CanPost(RusterUser user)
			{
				if (IsLocked) return false;

				var player = user.GetPlayer();
				if (player != null && player.IsAdmin) return true;

				return CanPost(user.Id);
			}
			public bool CanDelete(ulong userId, int postId)
			{
				var user = Instance.Data.GetUser(userId);
				var post = Instance.Data.GetPost(postId);
				var author = post.GetUser();

				if (user.IsAdmin() || user.IsModerator()) return true;
				if (!AllowDeleting || author.IsBot) return false;

				return post.UserId == user.Id || Id == user.Id;
			}
			public bool CanDelete(RusterUser user, RusterPost post)
			{
				var author = post.GetUser();

				if (user.IsAdmin() || user.IsModerator()) return true;
				if (!AllowDeleting || author.IsBot) return false;

				return post.UserId == user.Id || Id == user.Id;
			}
			public bool IsCommunity()
			{
				return Id == CommunityFeedId;
			}
			public bool IsMarketplace()
			{
				return Id == MarketplaceFeedId;
			}
			public bool Delete(RusterUser user, int postId, out string reason)
			{
				var post = Posts.FirstOrDefault(x => x.Id == postId);
				if (post == null) { reason = $"Post not found in feed {Id}"; return false; };
				if (!CanDelete(user, post)) { reason = $"You must be the owner of this post to delete it."; return false; };

				Posts.Remove(post);
				Instance.Data.Feeds.Remove(Instance.Data.GetFeed(post));

				reason = $"Successfully deleted.";
				return true;
			}
			public bool Delete(int postId, out string reason)
			{
				var post = Posts.FirstOrDefault(x => x.Id == postId);
				if (post == null) { reason = $"Post not found in feed {Id}"; return false; };

				Posts.Remove(post);
				Instance.Data.Feeds.Remove(Instance.Data.GetFeed(post));

				reason = $"Successfully deleted.";
				return true;
			}

			public RusterPost GetPost(int id)
			{
				return Posts.FirstOrDefault(x => x.Id == id);
			}

			public RusterUser GetOwner()
			{
				return Instance.Data.GetUser(Id);
			}
			public RusterFeed GetTemporaryCopy()
			{
				var feed = new RusterFeed
				{
					Id = Id,
					Title = Title,
					FeedType = FeedType,
					AllowAdverts = AllowAdverts
				};

				feed.Posts.AddRange(Posts);
				return feed;
			}

			public RusterHashtag[] GetHashtags()
			{
				if (Hashtags != null) return Hashtags.OrderByDescending(x => x.Instances).ToArray();

				var hashtags = Posts.SelectMany(x => x.GetHashtags()).ToList();
				var multiHashtags = Facepunch.Pool.GetList<RusterHashtag>();
				foreach (var hashtag in hashtags.ToArray())
				{
					var count = hashtags.Count(x => x.Filter == hashtag.Filter && x.FilterType == hashtag.FilterType);

					if (count > 1 && !multiHashtags.Any(x => x.Filter == hashtag.Filter && x.FilterType == hashtag.FilterType))
					{
						hashtags.RemoveAll(x => x.Filter == hashtag.Filter && x.FilterType == hashtag.FilterType);

						hashtag.Instances = count;
						multiHashtags.Add(hashtag);
						break;
					}
				}

				hashtags.AddRange(multiHashtags);
				var result = hashtags.OrderByDescending(x => x.Instances).ToArray();
				Facepunch.Pool.FreeList(ref hashtags);
				return result;
			}

			[Serializable]
			public class RusterPost
			{
				public int Id { get; set; }
				public ulong UserId { get; set; }
				public long Ticks { get; set; }
				public string Content { get; set; }
				public string EditedContent { get; set; }
				public string PhotoUrl { get; set; }
				public string PhotoTag { get; set; }
				public ulong CassetteId { get; set; }
				public string CassetteTitle { get; set; }
				public PostTypes PostType { get; set; } = PostTypes.None;
				public RusterAdvert Advert { get; set; }
				public RusterGif Gif { get; set; }
				public RusterPoll Poll { get; set; }
				public RusterMarketplaceListing MarketplaceListing { get; set; }
				public RusterLocation Location { get; set; } = new RusterLocation();
				public List<ulong> Likes { get; set; } = new List<ulong>();
				public List<ulong> Dislikes { get; set; } = new List<ulong>();
				public List<RusterHashtag> Hashtags { get; set; }
				public bool CanRate { get; set; } = true;
				public bool IsPinned { get; set; } = false;
				public bool EmbedPhoto { get; set; } = false;
				public float EmbedPhotoRatio { get; set; } = 1f;

				public enum PostTypes
				{
					None,
					Story
				}

				public RusterUser[] GetMentions()
				{
					var words = (string.IsNullOrEmpty(EditedContent) ? Content : EditedContent).Replace("\\n", "").EscapeRichText().Split(' ');
					var mentions = Facepunch.Pool.GetList<RusterUser>();
					var previousMention = (RusterUser)null;
					var previousWord = string.Empty;

					foreach (var word in words)
					{
						var formattedWord = string.Empty;
						var end = "";

						if (word.StartsWith("#"))
						{
							previousMention = null;
						}
						else if (word.StartsWith("@"))
						{
							foreach (var letter in word)
							{
								if (letter != '@' && letter != '!' && letter != '.' && letter != ',') formattedWord += letter;
								else if (!string.IsNullOrEmpty(formattedWord)) end += letter;
							}

							previousMention = Instance.Data.Users.FirstOrDefault(x => x.CurrentDisplayName == formattedWord);
							previousWord = formattedWord;

							if (previousMention != null && !mentions.Contains(previousMention))
							{
								mentions.Add(previousMention);
							}
						}
						else
						{
							if (Instance.Data.Users.Any(x => x.CurrentDisplayName == $"{previousWord} {word}".Trim()))
							{
								if (previousMention != null) mentions.Remove(previousMention);
								mentions.Add(Instance.Data.Users.FirstOrDefault(x => x.CurrentDisplayName == $"{previousWord} {word}".Trim()));
							}

							previousMention = null;
							previousWord = null;
						}
					}

					var result = mentions.ToArray();
					Facepunch.Pool.FreeList(ref mentions);
					return result;
				}
				public bool HasMentions()
				{
					return GetMentions().Length > 0;
				}
				public bool IsMentioned(RusterUser user)
				{
					foreach (var mention in GetMentions())
					{
						if (mention == user) return true;
					}

					return false;
				}
				public string GetContent()
				{
					var words = (string.IsNullOrEmpty(EditedContent) ? Content : EditedContent).Replace("\\n", "").EscapeRichText().Truncate(RusterBrowser.PostLength, "..", countElipsisLength: false).Split(' ');
					var body = new StringBody();
					var previousMention = (RusterUser)null;
					var previousWord = string.Empty;

					foreach (var word in words)
					{
						var formattedWord = string.Empty;
						var end = "";

						if (word.StartsWith("#"))
						{
							foreach (var letter in word)
							{
								if (char.IsLetterOrDigit(letter)) formattedWord += letter;
								else if (!string.IsNullOrEmpty(formattedWord)) end += letter;
							}

							formattedWord = $"<color={RusterBrowser.HashtagColor}>#{formattedWord}</color>{end}";

							body.Add(formattedWord);
							previousMention = null;
							previousWord = null;
						}
						else if (word.StartsWith("@"))
						{
							foreach (var letter in word)
							{
								if (letter != '@' && letter != '!' && letter != '.' && letter != ',') formattedWord += letter;
								else if (!string.IsNullOrEmpty(formattedWord)) end += letter;
							}

							var validMention = Instance.Data.Users.Any(x => x.CurrentDisplayName == formattedWord);
							previousMention = Instance.Data.Users.FirstOrDefault(x => x.CurrentDisplayName == formattedWord);
							previousWord = formattedWord;
							formattedWord = validMention ? $"<color={RusterBrowser.MentionColor}>{previousMention.GetDisplayName()}</color>{end}" : $"@{formattedWord}{end}";

							body.Add(formattedWord);
						}
						else
						{
							if (Instance.Data.Users.Any(x => x.CurrentDisplayName == $"{previousWord} {word}".Trim()))
							{
								var mention = Instance.Data.Users.FirstOrDefault(x => x.CurrentDisplayName == $"{previousWord} {word}".Trim());
								body.Remove($"@{previousWord}");
								body.Add($"<color={RusterBrowser.MentionColor}>{mention.GetDisplayName()}</color>");
							}
							else body.Add(word);

							previousMention = null;
							previousWord = null;
						}
					}

					return body.ToAppended();
				}
				public bool IsAdvert()
				{
					return Advert != null && IsMarketplaceListing();
				}
				public bool IsEmbedded()
				{
					return EmbedPhoto && !string.IsNullOrEmpty(PhotoUrl);
				}
				public bool IsMarketplaceListing()
				{
					return MarketplaceListing != null && !string.IsNullOrEmpty(MarketplaceListing.Shortname) && MarketplaceListing.GetSoldItemDefinition() != null;
				}
				public float AdvertTimeLeft()
				{
					if (Advert == null) return 0f;

					var now = DateTime.Now;
					var creation = new DateTime(Ticks);

					return Advert.DurationHours - (float)(now - creation).TotalHours;
				}
				public bool IsOverdue()
				{
					var time = DateTime.Now - new DateTime(Ticks);
					return time.TotalHours > Advert.DurationHours;
				}
				public bool IsExpired(int overHours)
				{
					var time = DateTime.Now - new DateTime(Ticks);
					return time.TotalHours > overHours;
				}
				public bool IsReply()
				{
					var feed = (RusterFeed)null;
					var post = Instance.Data.GetPost(Id, out feed);
					return feed.GetFeedType() == FeedTypes.Post;
				}
				public bool IsCassette()
				{
					return CassetteId != 0;
				}
				public bool IsEdited()
				{
					return !string.IsNullOrEmpty(EditedContent);
				}
				public bool IsPoll()
				{
					return Poll != null;
				}
				public bool IsGif()
				{
					return Gif != null;
				}
				public bool HasPollEnded()
				{
					var hours = (DateTime.Now - new DateTime(Ticks)).TotalHours;
					return hours >= Poll.DurationHours;
				}
				public bool HasUserVoted(RusterUser user)
				{
					return Poll.Choices.Any(x => x.Votes.Any(y => y == user.Id));
				}
				public bool CanEdit(RusterUser observer)
				{
					if (IsEdited()) return false;
					if (observer.Id == UserId) return true;

					return false;
				}
				public bool CanPin(RusterUser observer, RusterFeed feed)
				{
					var author = GetUser();

					if (observer == null) return author.IsAdmin() || author.IsModerator() || feed.Id == UserId;
					return observer.IsAdmin() || observer.IsModerator() || feed.Id == observer.Id;
				}

				public bool Pin(RusterUser observer, RusterFeed feed)
				{
					if (IsPinned || !CanPin(observer, feed)) return false;

					IsPinned = true;
					Instance.RusterAddons?.Call("RNETAPI_OnPostPinned", observer.Id, feed.Id, this);
					return true;
				}
				public bool Unpin(RusterUser observer, RusterFeed feed)
				{
					if (!IsPinned || !CanPin(observer, feed)) return false;

					IsPinned = false;
					Instance.RusterAddons?.Call("RNETAPI_OnPostUnpinned", observer.Id, feed.Id, this);
					return true;
				}

				public RusterUser[] GetLikes() { return Likes.Select(x => Instance.Data.GetUser(x)).ToArray(); }
				public RusterUser[] GetDislikes() { return Dislikes.Select(x => Instance.Data.GetUser(x)).ToArray(); }
				public RusterUser[] GetLikes(RusterServerViewer serverViewer) { if (serverViewer.CurrentServer == null) return GetLikes(); return Likes.Select(x => Instance.Data.GetUser(x, serverViewer)).ToArray(); }
				public RusterUser[] GetDislikes(RusterServerViewer serverViewer) { return GetDislikes(); }

				public RusterUser GetUser()
				{
					return Instance.Data.GetUser(UserId);
				}
				public RusterUser GetUser(RusterServerViewer serverViewer)
				{
					if (serverViewer.CurrentServer == null) return GetUser();
					return Instance.Data.GetUser(UserId, serverViewer);
				}
				public RusterFeed GetFeed()
				{
					foreach (var feed in Instance.Data.Feeds)
					{
						foreach (var post in feed.Posts)
						{
							if (post.Id == Id) return feed;
						}
					}

					return null;
				}
				public static int GetId()
				{
					var id = RandomEx.GetRandomInteger(1000, int.MaxValue);
					if (Instance.Data.Feeds.Any(x => x.Posts.Any(y => y.Id == id))) id = GetId();

					return id;
				}
				public static bool IsValidPostId(int id)
				{
					return id >= 1000 && id <= int.MaxValue;
				}

				public static RusterPost Create(RusterUser user, string message)
				{
					var post = new RusterPost
					{
						Id = GetId(),
						UserId = user.Id,
						Content = message,
						Ticks = DateTime.Now.Ticks
					};

					return post;
				}
				public bool Publish(RusterFeed feed, bool silent = false)
				{
					if (feed.Posts.Any(x => x.Id == Id || x == this) || !feed.CanPost(UserId)) return false;

					var author = GetUser();
					feed.Posts.Insert(0, this);

					if (!silent)
					{
						foreach (var friend in author.GetFriends())
						{
							if (!friend.Configuration.FriendsNotifications || IsMentioned(friend)) continue;

							var friendBrowser = Instance.GetBrowser(friend);
							friendBrowser.Notify(friendBrowser.GetPhrase("notif_t_friendpost"), friendBrowser.GetPhrase("notif_s_friendpost", author.GetDisplayName(false, observer: friend), feed.GetFeedTitle(friend), Content),
								duration: 15.0f,
								onRead: (RusterBrowser source) =>
								{
									source.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
									{
										source.DrawFullPost(feed, this);
									});
								}, openName: "Friends' Post");
						}

						foreach (var mentionedUser in GetMentions())
						{
							if (author.HasBlockedCommunication(mentionedUser) || mentionedUser == author) continue;

							var mentionedUserBrowser = Instance.GetBrowser(mentionedUser);
							mentionedUserBrowser.Notify(mentionedUserBrowser.GetPhrase("notif_t_mentionpost"), mentionedUserBrowser.GetPhrase("notif_s_mentionpost", author.GetDisplayName(false, observer: mentionedUser), feed.GetFeedTitle(mentionedUser), Content),
								duration: 15.0f,
								onRead: (RusterBrowser source) =>
								{
									source.Draw(RusterBrowser.PanelTypes.Post, onDraw: () =>
									{
										source.DrawFullPost(feed, this);
									});
								}, openName: "Mentioned Post");

							if (!mentionedUser.IsOnline())
							{
								mentionedUser.CreateNotification("You have been mentioned!", RusterUserNotification.NotificationTypes.Mention);
							}
						}
					}

					Instance.RusterAddons?.Call("RNETAPI_OnPostCreated", this, silent);
					return true;
				}

				public bool HasLiked(RusterUser user)
				{
					return Likes.Contains(user.Id);
				}
				public bool HasDisliked(RusterUser user)
				{
					return Dislikes.Contains(user.Id);
				}
				public void Like(RusterUser user)
				{
					if (Dislikes.Contains(user.Id)) Dislikes.RemoveAll(x => x == user.Id);
					if (Likes.Contains(user.Id)) { Likes.RemoveAll(x => x == user.Id); return; }

					Likes.Add(user.Id);
				}
				public void Dislike(RusterUser user)
				{
					if (Likes.Contains(user.Id)) Likes.RemoveAll(x => x == user.Id);
					if (Dislikes.Contains(user.Id)) { Dislikes.RemoveAll(x => x == user.Id); return; }

					Dislikes.Add(user.Id);
				}

				public bool CanGetForFree(RusterUser user)
				{
					return user.IsAdmin();
				}
				public bool CanBuy(RusterUser user)
				{
					if (CanGetForFree(user)) return true;

					var player = user.GetPlayer();
					if (player == null || (player.inventory.containerMain.IsFull() && player.inventory.containerBelt.IsFull())) return false;

					var browser = Instance.GetBrowser(user);
					var currency = browser.GetPlayerCurrency();
					if (currency < MarketplaceListing.GetTaxedPrice(browser)) return false;

					return true;
				}

				public RusterHashtag[] GetHashtags()
				{
					if (Hashtags != null) return Hashtags.ToArray();

					var hashtags = Facepunch.Pool.GetList<RusterHashtag>();
					foreach (var split in Content.Split(' '))
					{
						var correctSplit = string.Empty;

						foreach (var letter in split) if (char.IsLetterOrDigit(letter)) correctSplit += letter;
						correctSplit = correctSplit.Trim();

						if (!split.Contains("#")) continue;

						var existentHashtag = hashtags.FirstOrDefault(x => x.Filter.ToLower() == correctSplit.ToLower());

						if (existentHashtag != null) existentHashtag.Instances++;
						else hashtags.Add(new RusterHashtag(correctSplit, RusterHashtag.FilterTypes.Content));
					}
					var result = hashtags.ToArray();
					Facepunch.Pool.FreeList(ref hashtags);
					return result;
				}

				public void RefundListing()
				{
					if (MarketplaceListing == null || MarketplaceListing.IsPurchased) return;

					var user = GetUser();
					var player = user.GetPlayer();
					var amount = MarketplaceListing.WholeStack ? MarketplaceListing.Amount : MarketplaceListing.AmountLeft;

					if (amount > 0)
					{
						var item = MarketplaceListing.CreateItem();

						if (item != null)
						{
							item.amount = amount;
							player.GiveItem(item);

							player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain, false);
							player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, player.inventory.containerBelt, false);
						}
					}
				}

				public string GetPostType()
				{
					return IsReply() ? "Reply" : "Post";
				}

				[Serializable]
				public class RusterAdvert
				{
					public float DurationHours { get; set; }
				}

				[Serializable]
				public class RusterGif
				{
					public string Id { get; set; }
					public string Url { get; set; }
					public int Frames { get; set; }
					public bool IsFlipbook { get; set; }
				}

				[Serializable]
				public class RusterLocation
				{
					public string Name { get; set; }
					public RusterVector3 Position { get; set; }

					public bool IsValid() { return !string.IsNullOrEmpty(Name) && (Position == null ? true : Position.ToVector3() != Vector3.zero); }
				}

				[Serializable]
				public class RusterPoll
				{
					public float DurationHours { get; set; } = 8f;
					public List<Choice> Choices { get; set; } = new List<Choice>();

					public Choice GetWinningChoice()
					{
						return Choices.OrderByDescending(x => x.Votes.Count).FirstOrDefault();
					}

					public int GetTotalVotes()
					{
						return Choices.Sum(x => x.Votes.Count);
					}

					[Serializable]
					public class Choice
					{
						public string Text { get; set; }
						public List<ulong> Votes { get; set; } = new List<ulong>();

						public bool HasVoted(ulong userId)
						{
							return Votes.Any(x => x == userId);
						}
					}
				}

				[Serializable]
				public class RusterVector3
				{
					public float X { get; set; }
					public float Y { get; set; }
					public float Z { get; set; }

					public RusterVector3() { }
					public RusterVector3(Vector3 source)
					{
						X = source.x;
						Y = source.y;
						Z = source.z;
					}
					public Vector3 ToVector3()
					{
						return new Vector3(X, Y, Z);
					}
				}
			}
		}

		[Serializable]
		public class RusterMarketplaceListing
		{
			public string CustomName { get; set; }
			public string Shortname { get; set; }
			public ulong Skin { get; set; }
			public string SkinIconUrl { get; set; }
			public string Text { get; set; }
			public int Amount { get; set; } = 1;
			public int AmountLeft { get; set; } = 1;
			public int Price { get; set; } = 1;
			public ulong SubEntityId { get; set; } = 0;
			public bool IsPurchased { get; set; } = false;
			public bool WholeStack { get; set; } = true;
			public RusterCoupon Coupon { get; set; }

			[JsonIgnore, NonSerialized] public Item _Item;

			public void Clear()
			{
				Shortname = null;
				Skin = 0;
				Amount = 0;
			}
			public void SetSoldItem(Item item, ulong? customSubEntityData = null)
			{
				Shortname = item.info.shortname;
				Skin = item.skin;
				Amount = item.amount;
				CustomName = item.name;
				Text = item.text;
				SubEntityId = customSubEntityData ?? item.instanceData.subEntity.Value;
				_Item = item;
			}
			public bool CanRestock()
			{
				if (!WholeStack && AmountLeft != Amount) return true;

				return false;
			}
			public int GetTaxedPrice(RusterBrowser browser, bool add = false)
			{
				return (int)(GetPrice(browser) * (add ? -Instance.Config.Tax.GetValue() : Instance.Config.Tax.GetValue()));
			}
			public int GetPrice(RusterBrowser browser)
			{
				return WholeStack ? Price : Price * browser.CurrentStackAmount;
			}
			public int GetAmount(RusterBrowser browser)
			{
				return WholeStack ? Amount : browser.CurrentStackAmount;
			}

			public Item CreateItem(int amount = 1)
			{
				if (string.IsNullOrEmpty(Shortname)) return null;
				if (_Item != null) return _Item;

				var item = ItemManager.CreateByName(Shortname, WholeStack ? Amount.Clamp(1, int.MaxValue) : amount, Skin);
				if (item.instanceData != null) item.instanceData.subEntity = new NetworkableId(SubEntityId);
				item.name = CustomName;
				item.text = Text;
				item.MarkDirty();

				return item;
			}
			public ItemDefinition GetSoldItemDefinition() { return string.IsNullOrEmpty(Shortname) ? null : ItemManager.FindItemDefinition(Shortname); }
		}

		[Serializable]
		public class RusterFriendRequest
		{
			public ulong SentById { get; set; }
			public ulong SentToId { get; set; }
			public long Tick { get; set; }

			public RusterFriendRequest() { }
			public RusterFriendRequest(RusterUser sentBy, RusterUser sentTo)
			{
				SentById = sentBy.Id;
				SentToId = sentTo.Id;
				Tick = DateTime.Now.Ticks;
			}
		}

		[Serializable]
		public class RusterConversation
		{
			public int Id { get; set; }
			public string CustomTitle { get; set; }
			public bool IsLocked { get; set; } = false;
			public ConversationTypes ConversationType { get; set; } = ConversationTypes.None;
			public bool CanDelete { get; set; } = true;
			public bool CanReact { get; set; } = true;
			public bool CanManage { get; set; }
			public List<ulong> Users { get; set; } = new List<ulong>();
			public List<ulong> ViewerList { get; set; } = new List<ulong>();
			public List<RusterDirectMessage> Messages { get; set; } = new List<RusterDirectMessage>();

			public enum ConversationTypes
			{
				None,
				Team,
				Group
			}

			public static int GetId()
			{
				var id = RandomEx.GetRandomInteger(1000, int.MaxValue);
				if (Instance.Data.Conversations.Any(x => x.Id == id)) id = GetId();

				return id;
			}
			public void PostMessage(RusterDirectMessage message, bool notify = true)
			{
				Messages.Insert(0, message);

				message.Ticks = DateTick.Current.Ticks;

				var user = message.GetSender();

				foreach (var viewer in ViewerList)
				{
					if (viewer == user.Id) continue;

					var viewerUser = Instance.Data.GetUser(viewer);
					var viewerBrowser = Instance.GetBrowser(viewerUser);

					if (viewerUser.Configuration.DMNotifications)
					{
						if (viewerBrowser.PanelType == RusterBrowser.PanelTypes.DirectMessages &&
							viewerBrowser.ConversationId == Id &&
							string.IsNullOrEmpty(viewerBrowser.ConversationMessage))
						{
							viewerBrowser.DrawChatBalloonMessages();
						}
						else
						{
							if (viewerUser.HasBlockedCommunication(user)) continue;

							viewerBrowser.NotifyRustPlus($"Chat", $"{user.GetDisplayName(false, viewerUser)}: {message.Message}", forceSend: true);
							viewerBrowser.NotifyChat($"DM from <color=orange>{user.GetDisplayName(false, viewerUser)}</color>", message.Message);
						}
					}
				}

				var bots = Users.Where(x => Instance.Data.GetUser(x).IsBot);
				if (bots.Any() && !user.IsBot)
				{
					var split = message.Message.Split(' ');

					if (split.Length > 1)
					{
						var commandPrefix = split[0][0];
						var commandName = split[0].TrimStart(commandPrefix);
						var arguments = split.Skip(1).ToArray();

						foreach (var bot in bots)
						{
							var rusterBot = Instance.GetBot(bot);
							if (rusterBot == null || rusterBot.Prefix != commandPrefix) continue;

							var command = rusterBot.GetCommand(commandName);

							if (command == null)
							{
								PostMessage(new RusterDirectMessage(this, bot, $"Invalid command.", true), false);
								continue;
							}

							if (command.IsVoid) command.OnExecuteVoid?.Invoke(Id, bot, commandName, arguments);
							else
							{
								var result = command.OnExecuteString?.Invoke(Id, bot, commandName, arguments);
								if (!string.IsNullOrEmpty(result)) PostMessage(new RusterDirectMessage(this, bot, result, true), false);
							}
						}
					}
				}

				if (bots.Any()) Instance.RusterAddons?.Call("RNETAPI_OnBotDMSent", Id, message, !notify);
				else if (Users.Any(x => Instance.Data.GetUser(x).IsBot)) Instance.RusterAddons?.Call("RNETAPI_OnDMSent", Id, message, !notify);
			}
			public RusterDirectMessage GetMessage(int id)
			{
				return Messages.FirstOrDefault(x => x.Id == id);
			}
			public void MergeViewingList()
			{
				ViewerList.AddRange(Users.Where(x => !ViewerList.Contains(x)));
			}
			public RusterUser GetOtherUser(RusterUser user)
			{
				return Instance.Data.GetUser(Users.FirstOrDefault(x => x != user.Id));
			}
			public bool HasUnreadConversations(RusterUser user)
			{
				return UnreadMessageCount(user) > 0;
			}
			public int UnreadMessageCount(RusterUser user)
			{
				return Messages.Count(y => y.SenderId != user.Id && y.Status != RusterDirectMessage.StatusTypes.Read);
			}

			public bool IsCreator(ulong userId)
			{
				if (Users.Count == 0) return false;

				return Users[0] == userId;
			}

			public RusterConversation() { Id = GetId(); }

			[Serializable]
			public class RusterDirectMessage
			{
				public int Id { get; set; }
				public ulong SenderId { get; set; }
				public string Message { get; set; }
				public string Reaction { get; set; }
				public long Ticks { get; set; }
				public StatusTypes Status { get; set; } = StatusTypes.Sent;
				public bool IsSystemMessage { get; set; }
				public string PhotographUrl { get; set; }
				public ulong CassetteId { get; set; }

				public bool IsTradeRequest { get; set; }
				public bool IsTradeFinished { get; set; }

				public ulong FeedId { get; set; }
				public int PostId { get; set; }

				public bool IsPost()
				{
					return PostId != 0;
				}
				public bool IsTrade()
				{
					return IsTradeRequest;
				}
				public bool IsTradeCompleted()
				{
					return IsTradeFinished;
				}
				public float GetTimeSince()
				{
					return (float)(DateTime.Now - new DateTime(Ticks)).TotalSeconds;
				}

				public enum StatusTypes
				{
					Sent,
					Read
				}

				public static int GetId(RusterConversation conversation)
				{
					var id = RandomEx.GetRandomInteger(1000, int.MaxValue);
					if (conversation.Messages.Any(x => x.Id == id)) id = GetId(conversation);

					return id;
				}

				public RusterDirectMessage() { }
				public RusterDirectMessage(RusterConversation conversation, ulong senderId, string message, bool isSystemMessage = false)
				{
					Id = GetId(conversation);
					SenderId = senderId;
					Message = message;
					IsSystemMessage = isSystemMessage;
					Ticks = DateTime.Now.Ticks;
				}

				public void MarkRead()
				{
					Status = StatusTypes.Read;
				}
				public RusterUser GetSender()
				{
					return Instance.Data.GetUser(SenderId);
				}
			}
		}

		[Serializable]
		public class RusterBusinessCard
		{
			public ulong UserId { get; set; }

			public RusterUser GetUser()
			{
				return Instance.Data.GetUser(UserId);
			}
		}

		[Serializable]
		public class RusterFlipbook
		{
			public string Id { get; set; }
			public string ThumbnailUrl { get; set; }
			public int MaximumFrames { get; set; }
			public int Frames { get; set; } = 0;

			public RusterFlipbook() { }
			public RusterFlipbook(int maximumFrames)
			{
				Id = RandomEx.GetRandomString(20);
				MaximumFrames = maximumFrames;
			}
		}

		[Serializable]
		public class RusterStory : RusterFeed.RusterPost
		{
			public List<View> Views { get; set; } = new List<View>();

			public new static int GetId()
			{
				var id = RandomEx.GetRandomInteger(1000, int.MaxValue);
				if (Instance.Data.Stories.Any(x => x.Id == id)) id = GetId();

				return id;
			}

			public int GetViewCount()
			{
				return Views.Sum(x => x.Count);
			}
			public View GetOrCreateView(ulong userId)
			{
				var view = Views.FirstOrDefault(x => x.UserId == userId);
				if (view == null) Views.Add(view = new View { UserId = userId });

				return view;
			}

			[Serializable]
			public class View
			{
				public ulong UserId { get; set; }
				public int Count { get; set; }
			}
		}

		[Serializable]
		public class RusterCoupon
		{
			public string Code { get; set; }
			public float Discount { get; set; }
			public int MaximumUses { get; set; } = -1;
			public List<Use> Uses { get; set; } = new List<Use>();

			public int GetDiscountedPrice(int originalPrice)
			{
				return (int)Discount.Scale(0f, 100, originalPrice, 0f);
			}

			public bool IsDepleted()
			{
				if (MaximumUses <= 0) return false;

				return Uses.Count >= MaximumUses;
			}

			public bool DoUse(RusterUser user)
			{
				if (Uses.Count >= MaximumUses && MaximumUses != -1) return false;

				Uses.Insert(0, new Use(user));
				return true;
			}

			[Serializable]
			public class Use
			{
				public ulong UserId { get; set; }
				public long Ticks { get; set; }

				public Use() { }
				public Use(RusterUser user)
				{
					UserId = user.Id;
					Ticks = DateTick.Current.Ticks;
				}
			}
		}

		[Serializable]
		public class RusterTransaction
		{
			public ulong UserId { get; set; }
			public string Coupon { get; set; }
			public float Discount { get; set; }
			public string Item { get; set; }
			public string Name { get; set; }
			public ulong SkinId { get; set; }
			public int Amount { get; set; }
			public int Price { get; set; }
			public long Ticks { get; set; }

			public RusterTransaction() { }
			public RusterTransaction(RusterUser user, RusterMarketplaceListing listing, int amount, int price, bool usedCoupon = false)
			{
				UserId = user.Id;
				Coupon = usedCoupon ? listing.Coupon.Code : string.Empty;
				Discount = usedCoupon ? listing.Coupon.Discount : 0f;
				Item = listing.Shortname;
				Name = listing.CustomName;
				SkinId = listing.Skin;
				Amount = amount;
				Price = price;
				Ticks = DateTick.Current.Ticks;
			}
		}

		[Serializable]
		public class RusterGif
		{
			public bool Boomerang { get; set; } = false;
			public float Bleeding { get; set; } = 0.1f;
			public List<string> FrameUrls { get; set; } = new List<string>();

			public RusterGif() { }
			public RusterGif(float bleeding = 0.1f, bool boomerang = false, params string[] frameUrls)
			{
				Bleeding = bleeding;
				Boomerang = boomerang;
				FrameUrls.AddRange(frameUrls);
			}
		}

		[Serializable]
		public class RusterLicensedItem
		{
			public string DisplayName { get; set; }
			public string Value { get; set; }
			public int Price { get; set; }
			public string Author { get; set; } = "Raul-Sorin Sorban";
			public ItemTypes ItemType { get; set; } = ItemTypes.None;

			public enum ItemTypes
			{
				None,
				Avatar,
				Banner,
				Frame
			}

			public RusterLicensedItem() { }
			public RusterLicensedItem(string displayName, string value, int price = 100, ItemTypes itemType = ItemTypes.None)
			{
				DisplayName = displayName;
				Value = value;
				Price = price;
				ItemType = itemType;
			}
		}

		[Serializable]
		public class RusterLotteryEvent
		{
			public bool HasStarted { get; set; } = false;
			public long StartTick { get; set; }
			public Dictionary<Item, BasePlayer> Tickets { get; set; } = new Dictionary<Item, BasePlayer>();
			public KeyValuePair<Item, BasePlayer> Winner { get; set; }

			public bool IsParticipant(BasePlayer player)
			{
				foreach (var ticket in Tickets)
				{
					if (ticket.Value == player) return true;
				}

				return false;
			}

			public void StartEvent()
			{
				var pot = Instance.Data.Stock.Value;

				if (pot < Instance.Config.Lottery.TaxThreshold)
				{
					Instance.LotteryEvent = null;
					return;
				}

				StartTick = DateTick.Current.Ticks;

				foreach (var player in BasePlayer.activePlayerList)
				{
					var browser = Instance.GetBrowser(player);
					browser.Notify("Lottery Event Started", $"The pot contains <color=orange>{Instance.Config.Currency.GetValueName(browser, pot, true)}</color> worth in value! Buy tickets from the User Store now!");
				}
			}

			public void EndEvent()
			{
				var lotteryAccount = Instance.Data.GetLotteryAccount();
				var lotteryFeed = Instance.Data.GetFeed(lotteryAccount);
				var communityFeed = Instance.Data.GetCommunityFeed();
				var participants = Facepunch.Pool.GetList<BasePlayer>();

				Tickets.Clear();

				foreach (var player in BasePlayer.allPlayerList)
				{
					var hasTickets = false;
					var items = player.inventory.AllItems();
					foreach (var item in items)
					{
						if (item.skin == RusterLegitLotteryTicketSkinId ||
							item.skin == RusterLuckyCharmLotteryTicketSkinId ||
							item.skin == RusterIconicLotteryTicketSkinId)
						{
							Tickets.Add(item, player);
							hasTickets = true;
						}

						if (hasTickets && !participants.Contains(player))
						{
							participants.Add(player);
						}
					}
				}

				if (Tickets.Sum(x => x.Key.amount) < Instance.Config.Lottery.MinimumTickets)
				{
					var message = $"The minimum amount of totally purchased tickets has not been met! Cancelled the event.";

					foreach (var player in BasePlayer.activePlayerList)
					{
						var browser = Instance.GetBrowser(player);
						browser.Notify("Lottery Event", message);
					}

					RusterFeed.RusterPost.Create(lotteryAccount, message)
						.Publish(lotteryFeed, true);
					RusterFeed.RusterPost.Create(lotteryAccount, message)
						.Publish(communityFeed, true);

					Instance.LotteryEvent = null;
					return;
				}

				if (participants.Count < Instance.Config.Lottery.MinimumPlayers)
				{
					var message = $"There are not enough players that own Lottery Tickets. There must be at least {Instance.Config.Lottery.MinimumPlayers:n0}.";

					foreach (var player in BasePlayer.activePlayerList)
					{
						var browser = Instance.GetBrowser(player);
						browser.Notify("Lottery Event", message);
					}

					RusterFeed.RusterPost.Create(lotteryAccount, message)
						.Publish(lotteryFeed, true);
					RusterFeed.RusterPost.Create(lotteryAccount, message)
						.Publish(communityFeed, true);

					Instance.LotteryEvent = null;
					return;
				}

				Winner = Tickets.ElementAt(RandomEx.GetRandomInteger(0, Tickets.Count - 1));

				var totalTickets = Tickets.Where(x => x.Value == Winner.Value);
				var multiply = 1f;
				foreach (var totalTicket in totalTickets)
				{
					switch (totalTicket.Key.skin)
					{
						case RusterLegitLotteryTicketSkinId:
							multiply += 0.01f * totalTicket.Key.amount;
							break;

						case RusterLuckyCharmLotteryTicketSkinId:
							multiply += 0.03f * totalTicket.Key.amount;
							break;

						case RusterIconicLotteryTicketSkinId:
							multiply += 0.05f * totalTicket.Key.amount;
							break;
					}
				}

				var ticketsPrice = 0f;
				foreach (var ticket in Tickets)
				{
					switch (ticket.Key.skin)
					{
						case RusterLegitLotteryTicketSkinId:
							ticketsPrice += Instance.Config.Ads.AdvertLegitLotteryTicketPrice;
							break;

						case RusterLuckyCharmLotteryTicketSkinId:
							ticketsPrice += Instance.Config.Ads.AdvertLuckyCharmLotteryTicketPrice;
							break;

						case RusterIconicLotteryTicketSkinId:
							ticketsPrice += Instance.Config.Ads.AdvertIconicLotteryTicketPrice;
							break;
					}
				}

				var winnerUser = Instance.Data.GetUser(Winner.Value);
				var price = (int)((Instance.Data.Stock.Value * multiply) + ticketsPrice);
				winnerUser.Wallet += price;
				Instance.Data.Stock.Value = 0;

				foreach (var player in BasePlayer.activePlayerList)
				{
					if (!IsParticipant(player)) continue;

					var browser = Instance.GetBrowser(player);
					browser.Notify("Lottery Event", $"We've got a winner! {winnerUser.GetDisplayName(false)} won a total of {Instance.Config.Currency.GetValueName(Instance.GetBrowser(lotteryAccount), price)}!!! Congratulations!!");
				}

				RusterFeed.RusterPost.Create(lotteryAccount, $"We've got a winner! @{winnerUser.CurrentDisplayName} won a total of {Instance.Config.Currency.GetValueName(Instance.GetBrowser(lotteryAccount), price)}!!! Congratulations!!")
					.Publish(lotteryFeed, true);
				RusterFeed.RusterPost.Create(lotteryAccount, $"We've got a winner! @{winnerUser.CurrentDisplayName} won a total of {Instance.Config.Currency.GetValueName(Instance.GetBrowser(lotteryAccount), price)}!!! Congratulations!!")
					.Publish(communityFeed, true);

				foreach (var ticket in Tickets)
				{
					ticket.Key.Remove();
				}

				Instance.LotteryEvent = null;
			}

			public float TimeSinceStart => (float)(DateTime.Now - new DateTime(StartTick)).TotalSeconds;
		}

		[Serializable]
		public class RusterGiftCard
		{
			public int Value { get; set; }
			public ulong CreatorId { get; set; }

			public RusterGiftCard() { }
			public RusterGiftCard(RusterUser creator, int value)
			{
				CreatorId = creator == null ? 0 : creator.Id;
				Value = value;
			}
		}

		[Serializable]
		public class RusterModal
		{
			public string Title { get; set; }
			public string Description { get; set; }
			public Dictionary<string, RusterField> Fields { get; set; } = new Dictionary<string, RusterField>();

			public Dictionary<string, string> GetFieldValues()
			{
				var values = new Dictionary<string, string>();

				foreach (var field in Fields)
				{
					values.Add(field.Key, field.Value.Value);
				}

				return values;
			}

			[JsonIgnore] public Action OnSubmit { get; set; }
			[JsonIgnore] public Action OnCancel { get; set; }

			public void ApplyDefaults()
			{
				foreach (var field in Fields)
				{
					field.Value.Value = field.Value.DefaultValue;
				}
			}

			public RusterModal() { }
			public RusterModal(string title, string description, Action onCancel, Action onSubmit, params KeyValuePair<string, RusterField>[] fields)
			{
				Title = title;
				Description = description;

				foreach (var field in fields)
				{
					if (Fields.ContainsKey(field.Key))
					{
						Instance.Log($"A field with the same key already exists: {field.Key}");
						continue;
					}

					Fields.Add(field.Key, field.Value);
				}

				OnCancel = onCancel;
				OnSubmit = onSubmit;
			}

			[Serializable]
			public class RusterField
			{
				public string Title { get; set; } = "My Field";
				public string Description { get; set; }
				public bool IsRequired { get; set; } = false;
				public bool CanBeEmpty { get; set; } = false;
				public FieldTypes FieldType { get; set; } = FieldTypes.String;

				public string DefaultValue { get; set; }
				public string Value { get; set; }

				public RusterField() { }
				public RusterField(string title, string description, bool isRequired, FieldTypes fieldType, string defaultValue = null, bool canBeEmpty = false)
				{
					if (fieldType == FieldTypes.Number && string.IsNullOrEmpty(defaultValue))
					{
						defaultValue = "0";
					}

					Title = title;
					Description = description;
					IsRequired = isRequired;
					FieldType = fieldType;
					DefaultValue = defaultValue;
					Value = DefaultValue;
					CanBeEmpty = canBeEmpty;

				}

				public enum FieldTypes
				{
					String,
					Number,
					Toggle,
					Blank
				}
			}

			public string this[string key] => Fields[key].Value;
		}

		public class RusterEmoji
		{
			public string Name { get; set; }
			public string Shortname { get; set; }
			public string IconUrl { get; set; }
			public bool IsPremiumEmoji { get; set; }

			public RusterEmoji() { }
			public RusterEmoji(string name, string shortname, string iconUrl, bool isPremiumEmoji = false)
			{
				Name = name;
				Shortname = shortname;
				IconUrl = iconUrl;
				IsPremiumEmoji = isPremiumEmoji;
			}
		}

		public class RusterHashtag
		{
			public int Instances { get; set; } = 1;

			public string Filter { get; set; }
			public FilterTypes FilterType { get; set; }
			public enum FilterTypes
			{
				Content,
				ItemShortname,
				ItemName
			}

			public RusterHashtag() { }
			public RusterHashtag(string filter = null, FilterTypes filterType = FilterTypes.Content)
			{
				Filter = filter;
				FilterType = filterType;
			}

			public static RusterHashtag Content(string filter, int instances = 1)
			{
				return new RusterHashtag
				{
					Filter = filter,
					FilterType = FilterTypes.Content,
					Instances = instances
				};
			}
			public static RusterHashtag ItemShortname(string filter, int instances = 1)
			{
				return new RusterHashtag
				{
					Filter = filter,
					FilterType = FilterTypes.ItemShortname,
					Instances = instances
				};
			}
			public static RusterHashtag ItemName(string filter, int instances = 1)
			{
				return new RusterHashtag
				{
					Filter = filter,
					FilterType = FilterTypes.ItemName,
					Instances = instances
				};
			}

			public bool IsSame(RusterHashtag hashtag)
			{
				if (hashtag == null) return false;
				return Filter == hashtag.Filter && FilterType == hashtag.FilterType;
			}
		}

		public class RusterBot
		{
			public ulong Id { get; set; }
			public char Prefix { get; set; } = '!';
			public Plugin Plugin { get; set; }
			public List<Command> Commands { get; set; } = new List<Command>();

			public Command GetCommand(string command)
			{
				return Commands.FirstOrDefault(x => x.Name.ToLower() == command.ToLower().Trim());
			}

			public RusterBot() { }
			public RusterBot(ulong id, char prefix, Type type, Plugin plugin)
			{
				Id = id;
				Plugin = plugin;
				Prefix = prefix;

				var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

				foreach (var method in methods)
				{
					var parameters = method.GetParameters();

					if (parameters.Length != 4 ||
						 parameters[0].ParameterType != typeof(int) ||
						parameters[1].ParameterType != typeof(ulong) ||
						parameters[2].ParameterType != typeof(string) ||
						parameters[3].ParameterType != typeof(string[])) continue;

					var delegateVoid = Delegate.CreateDelegate(typeof(Command.CommandVoidExecutor), method, false) as Command.CommandVoidExecutor;
					var delegateString = Delegate.CreateDelegate(typeof(Command.CommandStringExecutor), method, false) as Command.CommandStringExecutor;

					var commandAttribute = new Command
					{
						BotId = id,
						Name = method.Name.ToLower().Trim(),
						IsVoid = true
					};

					if (method.ReturnType == typeof(string))
					{
						commandAttribute.OnExecuteString = (int conversationId, ulong botId, string command, string[] arguments) => { return delegateString.Invoke(conversationId, botId, command, arguments); };
						commandAttribute.IsVoid = false;
					}
					else commandAttribute.OnExecuteVoid = (int conversationId, ulong botId, string command, string[] arguments) => { delegateVoid.Invoke(conversationId, botId, command, arguments); };

					Commands.Add(commandAttribute);
				}
			}

			public class Command
			{
				public ulong BotId { get; set; }
				public string Name { get; set; }
				public bool IsVoid { get; set; }

				internal Action<int, ulong, string, string[]> OnExecuteVoid { get; set; }
				internal Func<int, ulong, string, string[], string> OnExecuteString { get; set; }

				public delegate void CommandVoidExecutor(int conversationId, ulong botId, string command, string[] arguments);
				public delegate string CommandStringExecutor(int conversationId, ulong botId, string command, string[] arguments);
			}
		}

		#endregion

		#region Helpers

		private readonly Regex RegexAvatar = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");
		private readonly Regex RegexUsername = new Regex(@"<steamID><!\[CDATA\[(.*)\]\]></steamID>");

		public Item CreateBusinessCard(ulong userId)
		{
			var user = Data.GetUser(userId);
			var item = ItemManager.CreateByName("paper", skin: Instance.GetProConfig(nameof(RusterBusinessCardSkinId), RusterBusinessCardSkinId));
			item.name = string.Format(Instance.GetProConfig(nameof(RusterBusinessCardSkinName), RusterBusinessCardSkinName), user.GetDisplayName(false));
			item.text = JsonConvert.SerializeObject(new RusterBusinessCard() { UserId = user.Id });

			return item;
		}
		public void GiveBusinessCard(BasePlayer player, ulong userId)
		{
			player.GiveItem(CreateBusinessCard(userId));
		}

		public static void SendEffectTo(BasePlayer player, string effect)
		{
			if (player == null) return;

			var effectInstance = new Effect();
			effectInstance.Init(Effect.Type.Generic, player, 0, Vector3.up, Vector3.zero);
			effectInstance.pooledstringid = StringPool.Get(effect);
			NetWrite netWrite = Net.sv.StartWrite();
			netWrite.PacketID(Message.Type.Effect);
			effectInstance.WriteToStream(netWrite);
			netWrite.Send(new SendInfo(player.net.connection));
			effectInstance.Clear();
		}

		private void GetSteamProfileInfo(ulong userId, Action<string, string> callback)
		{
			if (callback == null) return;

			Instance.WebRequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1", null, (code, response) =>
			{
				try
				{
					if (code != 200 || response == null)
						return;

					callback.Invoke(RegexAvatar.Match(response).Groups[1].ToString(), RegexUsername.Match(response).Groups[1].ToString());
				}
				catch { }
			}, Instance);
		}
		private void GetSteamWorkshopIcon(ulong skinId, Action<string> callback)
		{
			Instance.WebRequest.Enqueue($"https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/",
				$"publishedfileids[0]={skinId}&itemcount=1",
				(code, response) =>
				{
					try
					{
						if (code != 200 || response == null)
							return;

						var json = JObject.Parse(response);
						callback?.Invoke(json["response"]["publishedfiledetails"][0]["preview_url"].ToObject<string>());
					}
					catch { }
				}, Instance, RequestMethod.POST);
		}
		public string GetRusterWatermarkPath()
		{
			Log($"Fetching Ruster.NET watermark...");

			var folder = GetTempFolder();
			var file = $"{folder}{Path.DirectorySeparatorChar}ruster_watermark.jpg";
			global::RusterNET.Utils.CheckOrUpdateLogo(file, RusterBrowser.RusterLogo);

			return file;
		}
		public string GetRusterMarketplaceWatermarkPath()
		{
			Log($"Fetching Ruster.NET Marketplace watermark...");

			var folder = GetTempFolder();
			var file = $"{folder}{Path.DirectorySeparatorChar}ruster_marketplace_watermark.jpg";
			global::RusterNET.Utils.CheckOrUpdateLogo(file, RusterBrowser.RusterMarketplaceLogo);

			return file;
		}

		public static float TextDifferencePercentage(string value, string otherValue)
		{
			if (value == null) value = string.Empty;
			if (otherValue == null) otherValue = string.Empty;

			var count = value.Length > otherValue.Length ? value.Length : otherValue.Length;
			var hits = 0;
			var iteratorOne = 0;
			var iteratorTwo = 0;

			for (iteratorOne = 0; iteratorOne <= value.Length - 1; iteratorOne++)
			{
				if (value[iteratorOne] == ' ')
				{
					iteratorOne += 1; iteratorTwo = otherValue.IndexOf(' ', iteratorTwo) + 1; hits += 1;
				}
				while (iteratorTwo < otherValue.Length && otherValue[iteratorTwo] != ' ')
				{
					if (value[iteratorOne] == otherValue[iteratorTwo])
					{
						hits += 1;
						iteratorTwo += 1;
						break;
					}
					else
						iteratorTwo += 1;
				}
				if (!(iteratorTwo < otherValue.Length && otherValue[iteratorTwo] != ' '))
					iteratorTwo -= 1;
			}
			return ((float)Math.Round(hits / (double)count, 2)).Scale(0f, 1f, 100f, 0f);
		}
		public static TimeZoneInfo GetTimeZone(string webTimeZone)
		{
			var split = webTimeZone.Split('/');
			var city = split[1].Replace("_", " ").ToLower().Trim();
			return TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.DisplayName.ToLower().Contains(city) || x.DaylightName.ToLower().Contains(city) || x.StandardName.ToLower().Contains(city));
		}

		#endregion

		#region Config 

		public Core.Configuration.DynamicConfigFile ConfigFile { get; set; }
		public Core.Configuration.DynamicConfigFile DataFile { get; set; }

		public new RootConfig Config { get; set; } = new RootConfig();
		public RootData Data { get; set; } = new RootData();

		[Serializable]
		public class RootConfig
		{
			public int LogLevel { get; set; } = -1;
			public DataTypes DataType { get; set; } = DataTypes.JSON;

			public enum DataTypes
			{
				JSON,
				SQL
			}

			[JsonProperty("UniqueId (Must not be null/empty)")]
			public string UniqueId { get; set; } = "defaultid";

			public FeaturesConfig Features { get; set; } = new FeaturesConfig();

			[JsonProperty("Ruster.NET Internet")]
			public InternetConfig Internet { get; set; } = new InternetConfig();
			public AdConfig Ads { get; set; } = new AdConfig();
			public TaxConfig Tax { get; set; } = new TaxConfig();
			public AdvertConfig Advert { get; set; } = new AdvertConfig();
			public MarketplaceConfig Marketplace { get; set; } = new MarketplaceConfig();
			public StoriesConfig Stories { get; set; } = new StoriesConfig();
			public TradeConfig Trade { get; set; } = new TradeConfig();
			public PollsConfig Polls { get; set; } = new PollsConfig();
			public LicenseConfig License { get; set; } = new LicenseConfig();
			public LotteryConfig Lottery { get; set; } = new LotteryConfig();
			public CommandsConfig Commands { get; set; } = new CommandsConfig();
			public LookConfig Look { get; set; } = new LookConfig();
			public DMConfig DMs { get; set; } = new DMConfig();
			public NotificationsConfig Notifications { get; set; } = new NotificationsConfig();
			public SoundsConfig Sounds { get; set; } = new SoundsConfig();
			public PhotographUploadConfig PhotographUpload { get; set; } = new PhotographUploadConfig();
			public CooldownConfig Cooldown { get; set; } = new CooldownConfig();
			public CurrencyConfig Currency { get; set; } = new CurrencyConfig();
			public ProfanityConfig Profanity { get; set; } = new ProfanityConfig();
			public SqlConfig Sql { get; set; } = new SqlConfig();
			public LocalisationConfig Localisation { get; set; } = new LocalisationConfig();
			public WipeConfig Wipe { get; set; } = new WipeConfig();

			public class AdConfig
			{
				[JsonProperty("Show Ruster.NET-Adverts in Marketplace")]
				public bool ShowRusterNETAdvertsInMarketplace { get; set; } = true;
				public int AdvertPrice24h { get; set; } = 5000;
				public float AdvertPriceMultiplier1w { get; set; } = 6.5f;
				public int AdvertShortFlipbookPrice { get; set; } = 50;
				public int AdvertMediumFlipbookPrice { get; set; } = 85;
				public int AdvertLongFlipbookPrice { get; set; } = 150;
				public int FlipbookResetPrice { get; set; } = 25;
				public int AdvertLegitLotteryTicketPrice { get; set; } = 100;
				public int AdvertLuckyCharmLotteryTicketPrice { get; set; } = 500;
				public int AdvertIconicLotteryTicketPrice { get; set; } = 1500;
			}
			public class LookConfig
			{
				public bool DisableBlur { get; set; } = false;
				public float BackgroundOpacity { get; set; } = 0.85f;
				public string AdminNameColor { get; set; } = "#de3535";
				public string ModeratorNameColor { get; set; } = "#a3de35";
				public string DeveloperNameColor { get; set; } = "#ded035";
			}
			public class NotificationsConfig
			{
				public float KeepUserNotificationsForHours { get; set; } = 72;
				public float VerticalOffset { get; set; } = 0;
			}
			public class FeaturesConfig
			{
				public bool EnableMarketplace { get; set; } = true;
				public bool EnableLocation { get; set; } = true;
				public bool EnableAddPhoto { get; set; } = true;
				public bool EnableAddCassette { get; set; } = true;
				public bool EnableAddGifPhoto { get; set; } = true;
				public bool EnableRecordMemo { get; set; } = true;
				public bool EnableFlipbook { get; set; } = true;

				public bool AutoHolidayMode { get; set; } = true;

				[JsonProperty("HolidayMode (0 = None, 1 = Halloween)")]
				public HolidayModes HolidayMode { get; set; } = HolidayModes.None;

				public HolidayModes GetHolidayMode()
				{
					if (AutoHolidayMode)
					{
						var date = Date.Current;

						if (date.Month == 10)
							return HolidayMode = HolidayModes.Halloween;
						if (date.Month == 12)
							return HolidayMode = HolidayModes.Christmas;
					}

					HolidayMode = HolidayModes.None;
					return HolidayMode;
				}

				public enum HolidayModes
				{
					None,
					Halloween,
					Christmas
				}
			}
			public class PollsConfig
			{
				[JsonProperty("Default Duration (in hours)")]
				public float DefaultDuration { get; set; } = 8f;
			}
			public class DMConfig
			{
				public bool MustBeFriendsToDM { get; set; } = true;
				public float DeleteOwnMessagesCooldown { get; set; } = 30 * 60;
				public bool AutoTeamGroups { get; set; } = true;
			}
			public class SoundsConfig
			{
				public string FFMPEGPath { get; set; } = string.Empty;

				public bool PlayStartup { get; set; } = true;
				public bool PlayBeeps { get; set; } = true;
				public bool PlayLikes { get; set; } = true;
				public bool PlayDislikes { get; set; } = true;
				public bool PlayVibrations { get; set; } = true;

				public bool IsValid()
				{
					return !string.IsNullOrEmpty(FFMPEGPath) && OsEx.File.Exists(FFMPEGPath);
				}
			}
			public class CommandsConfig
			{
				public string RusterCommand { get; set; } = "ruster";
				public string RusterLoginCommand { get; set; } = "rusterlogin";
				public string LaunchRuster { get; set; } = "launchruster";
				public string CloseRuster { get; set; } = "closeruster";
				public string GetRuster { get; set; } = "getruster";
				public string Get24hAdvert { get; set; } = "get25hadvert";
				public string Get1wAdvert { get; set; } = "get1wadvert";

				public string RusterAllNotifications { get; set; } = "rusteran";
				public string RusterPushNotifications { get; set; } = "rusterpush";
				public string RusterFriendsNotifications { get; set; } = "rusterfn";
				public string RusterRustPlusNotifications { get; set; } = "rusterrp";
				public string RusterChatNotifications { get; set; } = "rustercn";

				public string PinRusterFM { get; set; } = "pinrusterfm";
				public string RusterPrivacyMode { get; set; } = "rusterpm";
				public string RusterRatio { get; set; } = "rusterratio";
			}
			public class PhotographUploadConfig
			{
				public int MinimumFlipbookFrames { get; set; } = 3;
				public string ImgurClientId { get; set; } = "";
			}
			public class CooldownConfig
			{
				public float ButtonPress { get; set; } = 0.5f;
				public float BusinessCardCreation { get; set; } = 60f;
				public float RustPlusNotifications { get; set; } = 4f;
			}
			public class CurrencyConfig
			{
				[JsonProperty("Currency Type (0 = Item, 1 = ServerRewards, 2 = Economics, 3 = Other")]
				public CurrencyTypes CurrencyType { get; set; } = CurrencyTypes.Item;

				public string ItemShortname { get; set; } = "scrap";
				public ulong ItemSkinId { get; set; } = 0;

				public string CustomCurrencyName { get; set; } = null;
				public string CustomCurrencyPluralName { get; set; } = null;

				public PluginSettings OtherSettings { get; set; } = new PluginSettings();

				public enum CurrencyTypes
				{
					Item,
					ServerRewards,
					Economics,
					Other
				}

				public string GetName(RusterBrowser browser)
				{
					switch (CurrencyType)
					{
						case CurrencyTypes.ServerRewards:
							return "ServerRewards";

						case CurrencyTypes.Economics:
							return "Economics";

						case CurrencyTypes.Other:
							return OtherSettings.FullName;

						default:
							var definition = ItemManager.FindItemDefinition(ItemShortname);
							return browser.GetPhrase(definition);
					}
				}
				public string GetValueName(RusterBrowser browser, int amount, bool useBoldValue = false)
				{
					var customCurrencyName = amount.Plural(CustomCurrencyName, CustomCurrencyPluralName);

					switch (CurrencyType)
					{
						case CurrencyTypes.ServerRewards:
							return $"{(useBoldValue ? "<b>" : "")}{amount:n0}{(useBoldValue ? "</b>" : "")} {(string.IsNullOrEmpty(customCurrencyName) ? "RP" : customCurrencyName)}";

						case CurrencyTypes.Economics:
							return $"{(useBoldValue ? "<b>" : "")}{amount:n0}{(useBoldValue ? "</b>" : "")} {(string.IsNullOrEmpty(customCurrencyName) ? "Coins" : customCurrencyName)}";

						case CurrencyTypes.Other:
							return $"{(useBoldValue ? "<b>" : "")}{amount:n0}{(useBoldValue ? "</b>" : "")} {(string.IsNullOrEmpty(customCurrencyName) ? OtherSettings.ShortName : customCurrencyName)}";

						default:
							if (!string.IsNullOrEmpty(customCurrencyName)) return $"{(useBoldValue ? "<b>" : "")}{amount:n0}{(useBoldValue ? "</b>" : "")} {customCurrencyName}";

							var definition = ItemManager.FindItemDefinition(ItemShortname);
							return $"{(useBoldValue ? "<b>" : "")}{amount:n0}{(useBoldValue ? "</b>" : "")} {browser?.GetPhrase(definition)}";
					}
				}
				public string GetShortname(RusterBrowser browser)
				{
					var customCurrencyName = string.IsNullOrEmpty(Instance.Config.Currency.CustomCurrencyPluralName) ? Instance.Config.Currency.CustomCurrencyName : Instance.Config.Currency.CustomCurrencyPluralName;

					switch (CurrencyType)
					{
						case CurrencyTypes.ServerRewards:
							return string.IsNullOrEmpty(customCurrencyName) ? "RP" : customCurrencyName;

						case CurrencyTypes.Economics:
							return string.IsNullOrEmpty(customCurrencyName) ? "Coins" : customCurrencyName;

						case CurrencyTypes.Other:
							return string.IsNullOrEmpty(customCurrencyName) ? OtherSettings.ShortName : customCurrencyName;

						default:
							if (!string.IsNullOrEmpty(customCurrencyName)) return customCurrencyName;

							var definition = ItemManager.FindItemDefinition(ItemShortname);
							return browser.GetPhrase(definition);
					}
				}

				public class PluginSettings
				{
					public string PluginName { get; set; } = "MyCurrencyPlugin";

					[JsonProperty("TypeMode (0 = Int, 1 = Double, 2 = Float)")]
					public TypeModes TypeMode { get; set; } = TypeModes.Int;

					public string FullName { get; set; } = "My Bank";
					public string ShortName { get; set; } = "cc";

					public string DepositMethod { get; set; } = "Deposit";
					public string WithdrawMethod { get; set; } = "Withdraw";
					public string BalanceMethod { get; set; } = "Balance";

					public enum TypeModes
					{
						Int,
						Double,
						Float
					}
				}
			}
			public class LocalisationConfig
			{
				public bool AutoUpdatePhrases { get; set; } = false;
				public string LanguagesAPI { get; set; } = "https://raw.githubusercontent.com/raulssorban/rusternet-lang/main/languages.json";
				public string LocaleAPI { get; set; } = "https://raw.githubusercontent.com/raulssorban/rusternet-lang/main/{0}/RusterNET.json";
				public string DefaultLanguage { get; set; } = "en-GB";
				public Language[] Languages { get; set; } = new Language[] { new Language() };

				public Language GetLanguage(string id) => Languages.FirstOrDefault(x => x.Id == id);

				public void UpdatePhrases(bool reloadPlugin = true)
				{
					var counter = 0;

					Instance.webrequest.Enqueue(LanguagesAPI, string.Empty, (int code, string data) =>
					{
						Languages = JsonConvert.DeserializeObject<JObject>(data)["Languages"].ToObject<Language[]>();

						foreach (var language in Languages)
						{
							var url = string.Format(LocaleAPI, language.Id);
							Instance.webrequest.Enqueue(url, string.Empty, (int code2, string data2) =>
							{
								counter++;

								if (code2 == 200)
								{
									var folder = $"{Interface.Oxide.InstanceDirectory}{Path.DirectorySeparatorChar}lang{Path.DirectorySeparatorChar}{language.Id}";
									var file = $"{folder}{Path.DirectorySeparatorChar}RusterNET.json";
									OsEx.Folder.Create(folder);
									OsEx.File.Create(file, data2);

									Instance.Log($"Updated {language.Name} [{language.Id}].");
								}
								else
								{
									Instance.Log($"Failed updating {language.Name} [{language.Id}].");
								}

								if (counter == Languages.Length)
								{
									Instance.Log("Finished updating languages.");

									if (reloadPlugin)
									{
										Instance.OnServerSave();
										Interface.Oxide.RootPluginManager.RemovePlugin(Instance);
										Interface.Oxide.RootPluginManager.AddPlugin(Instance);
										Instance.OnServerInitialized();
									}
								}
							}, Instance);
						}
					}, Instance);
				}

				public class Language
				{
					public string Name { get; set; } = "English (UK)";
					public string Id { get; set; } = "en-GB";
					public string FlagUrl { get; set; } = "https://findicons.com/files/icons/282/flags/48/united_kingdom_great_britain.png";
				}
			}
			public class MarketplaceConfig
			{
				[JsonProperty("MaximumStackSizeEachItem  (-1 = Default stacksize for the item)")]
				public int MaximumStackSizeEachItem { get; set; } = 500;
				[JsonProperty("MaximumStackSizeWholeStack  (-1 = Default stacksize for the item)")]
				public int MaximumStackSizeWholeStack { get; set; } = 100000;
				public int MinimumPrice { get; set; } = 0;
				public int MaximumPrice { get; set; } = 2500;
				public string[] BlacklistedItems { get; set; } = new string[] { };

				[JsonProperty("Refund on Delete")]
				public bool RefundOnDelete { get; set; } = false;

				[JsonProperty("Refund on expired Advert")]
				public bool RefundOnExpiredAdvert { get; set; } = true;

				[JsonProperty("ListingSellingMode (0 = Both, 1 = Each Item, 2 = Whole Stack)")]
				public ListingSellingModes ListingSellingMode { get; set; } = ListingSellingModes.Both;

				public bool ValidateWholeStack(bool wantWholeStack)
				{
					switch (ListingSellingMode)
					{
						case ListingSellingModes.EachItem:
							return false;

						case ListingSellingModes.WholeStack:
							return true;
					}

					return wantWholeStack;
				}

				public enum ListingSellingModes
				{
					Both,
					EachItem,
					WholeStack
				}
			}
			public class ProfanityConfig
			{
				public string[] BannedWords { get; set; } = new string[] { };
				public string[] BannedWordReplacements { get; set; } = new string[] { };

				public string DoProfanityCheck(string originalText)
				{
					var message = originalText;
					if (string.IsNullOrEmpty(message)) return originalText;

					var replacements = Instance.Config.Profanity.BannedWordReplacements;

					foreach (var bannedWord in Instance.Config.Profanity.BannedWords)
					{
						var replacement = replacements[RandomEx.GetRandomInteger(0, replacements.Length - 1)];
						message = message?.Replace(bannedWord, replacement);
					}

					return message;
				}
			}
			public class SqlConfig
			{
				public int Port { get; set; } = 3306;
				public string Hostname { get; set; } = "localhost";
				public string Database { get; set; } = "RusterNET";
				public string Username { get; set; } = "root";
				public string Password { get; set; }
				public string Table { get; set; } = "RusterNET";
			}
			public class TaxConfig
			{
				public float Value { get; set; } = 0.1f;

				public float GetValue()
				{
					return Value + 1f;
				}
				public string GetPercentage()
				{
					return $"{Value.Scale(0f, 1f, 0, 100)}%";
				}

				public bool IsThereTax()
				{
					return Value != 1f;
				}
			}
			public class InternetConfig
			{
				public bool Enable { get; set; } = true;
			}
			public class StoriesConfig
			{
				public int MaximumSimultaneousPosts { get; set; } = 5;
			}
			public class WipeConfig
			{
				[JsonProperty("Mode (0 = None, 1 = Posts with Audio, 2 = Marketplace Listings, 3 = Everything)")]
				public Modes Mode { get; set; } = Modes.MarketplaceListings;

				public enum Modes
				{
					None,
					PostsWithAudio,
					MarketplaceListings,
					Everything
				}
			}
			public class AdvertConfig
			{
				public RusterCoupon Advert24hCoupon { get; set; }
				public RusterCoupon Advert1wCoupon { get; set; }
				public RusterCoupon AdvertShortFlipbookCoupon { get; set; }
				public RusterCoupon AdvertMediumFlipbookCoupon { get; set; }
				public RusterCoupon AdvertLongFlipbookCoupon { get; set; }
			}
			public class LicenseConfig
			{
				public string Legend => $"ItemTypes: 0 = None, 1 = Avatar, 2 = Banner, 3 = Frame";
				public RusterLicensedItem[] Items { get; set; } = new RusterLicensedItem[]
				{
					new RusterLicensedItem { DisplayName = "Arctic", Price = 750, ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940030093223407646/ruster_avatar_01.jpg" },
					new RusterLicensedItem { DisplayName = "Lo-ck", Price = 250, ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940089316556820510/18518237_1854250414898287_5588811981481567702_o.png" },
					new RusterLicensedItem { DisplayName = "Nomad", Price = 550, ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940330660546109500/ruster_vendor_rod_female.png" },
					new RusterLicensedItem { DisplayName = "Sun", Price = 300, ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940330669307986030/ruster_vendor_rod_female_1.png" },
					new RusterLicensedItem { DisplayName = "Focus", Price = 300, ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940358982483185704/ruster_vendor_rod_female_2.png" },
					new RusterLicensedItem { DisplayName = "Chick", Price = 300, ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940358989705805824/ruster_vendor_rod_female_3.png" },
					new RusterLicensedItem { DisplayName = "UK Grime MC", Price = 600, Author = "David", ItemType = RusterLicensedItem.ItemTypes.Avatar, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941164416827805716/avatar.png" },


					new RusterLicensedItem { DisplayName = "Codefling", Price = 300, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940232067147509760/RustClient_yhjZdgAtpR_3_1.png" },
					new RusterLicensedItem { DisplayName = "Mask", Price = 100, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940233528828895282/RustClient_yhjZdgAtpR_3_2.png" },
					new RusterLicensedItem { DisplayName = "Trumpee", Price = 50, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940234370462142504/RustClient_yhjZdgAtpR_3_3.png" },
					new RusterLicensedItem { DisplayName = "Balloon", Price = 50, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940234978728497232/RustClient_yhjZdgAtpR_3_4.png" },
					new RusterLicensedItem { DisplayName = "Gunshot", Price = 600, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940300617308581978/RustClient_yhjZdgAtpR_3_4_1.png" },
					new RusterLicensedItem { DisplayName = "Friday House", Price = 550, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940302146291765349/RustClient_yhjZdgAtpR_3_4_3.png" },
					new RusterLicensedItem { DisplayName = "Bucket", Price = 300, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940302470578602064/RustClient_yhjZdgAtpR_3_4_4.png" },
					new RusterLicensedItem { DisplayName = "Franko", Price = 200, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940304762660847656/RustClient_yhjZdgAtpR_3_4_7.png" },
					new RusterLicensedItem { DisplayName = "Search", Price = 250, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940304762967064656/RustClient_yhjZdgAtpR_3_4_6.png" },
					new RusterLicensedItem { DisplayName = "Evil", Price = 250, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940304763281612800/RustClient_yhjZdgAtpR_3_4_5.png" },
					new RusterLicensedItem { DisplayName = "Wolf", Price = 300, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940304763571011704/RustClient_yhjZdgAtpR_3_4_8.png" },
					new RusterLicensedItem { DisplayName = "Valid", Price = 200, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940334305480155186/RustClient_yhjZdgAtpR_3_7.png" },
					new RusterLicensedItem { DisplayName = "Summer", Price = 350, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940336190974992484/RustClient_yhjZdgAtpR_3_6.png" },
					new RusterLicensedItem { DisplayName = "Country", Price = 150, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940336194980556820/RustClient_yhjZdgAtpR_3_8.png" },
					new RusterLicensedItem { DisplayName = "Treasure", Price = 150, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940336197866233876/RustClient_yhjZdgAtpR_3_9.png" },
					new RusterLicensedItem { DisplayName = "Train Hunt", Price = 200, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940353119861215232/RustClient_yhjZdgAtpR_3_10.png" },
					new RusterLicensedItem { DisplayName = "Cards", Price = 350, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940353120159031306/RustClient_yhjZdgAtpR_3_11.png" },
					new RusterLicensedItem { DisplayName = "Chick 2", Price = 350, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940358985645711420/RustClient_yhjZdgAtpR_3_12.png" },
					new RusterLicensedItem { DisplayName = "Sub", Price = 400, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941316558377091082/RustClient_yhjZdgAtpR_3_8_2.png" },
					new RusterLicensedItem { DisplayName = "Officials", Price = 500, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941335948619284580/RustClient_yhjZdgAtpR_3_8_4.png" },
					new RusterLicensedItem { DisplayName = "Frankenstein", Price = 350, ItemType = RusterLicensedItem.ItemTypes.Banner, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941335948287946782/RustClient_yhjZdgAtpR_3_8_3.png" },

					new RusterLicensedItem { DisplayName = "Tape", Price = 0, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940408516148080701/ruster_avatar_frame.png" },
					new RusterLicensedItem { DisplayName = "Winter", Price = 50, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940417157488082985/ruster_avatar_frame_1.png" },
					new RusterLicensedItem { DisplayName = "Cyber", Price = 50, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940417521260048414/ruster_avatar_frame_2.png" },
					new RusterLicensedItem { DisplayName = "Kitten", Price = 100, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940417713048780810/ruster_avatar_frame_3.png" },
					new RusterLicensedItem { DisplayName = "Rainbow", Price = 50, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940421494700388382/ruster_avatar_frame_4.png" },
					new RusterLicensedItem { DisplayName = "Camera", Price = 100, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940432970605543514/ruster_avatar_frame_6.png" },
					new RusterLicensedItem { DisplayName = "Witness Stand", Price = 150, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940432970827853824/ruster_avatar_frame_5.png" },
					new RusterLicensedItem { DisplayName = "Laurel Leaf", Price = 500, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940432971029164153/ruster_avatar_frame_7.png" },
					new RusterLicensedItem { DisplayName = "TV", Price = 250, ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/940433479320084540/ruster_avatar_frame_8.png" },
					new RusterLicensedItem { DisplayName = "Scuba", Price = 150, Author = "Facepunch", ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941367748095791124/666a447f96867d1cbda6b586346e68974b6696e5.png" },
					new RusterLicensedItem { DisplayName = "Home", Price = 400,Author = "Facepunch", ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941367761999904818/839715911a346a8f9d4ca9c64c5840b9c7c9979a.png" },
					new RusterLicensedItem { DisplayName = "Metal", Price = 300,Author = "Facepunch", ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941367776130506812/f7d6755a87117708e455954caaf89fc1ead2fcae.png" },
					new RusterLicensedItem { DisplayName = "Iron", Price = 300,Author = "Facepunch", ItemType = RusterLicensedItem.ItemTypes.Frame, Value = "https://cdn.discordapp.com/attachments/844914604080889867/941367788050714674/118dd05387f411cbce73b7ef65f25428088b6b2a.png" },

				};

				public RusterLicensedItem GetLicense(string value)
				{
					var license = (RusterLicensedItem)null;
					if (IsLicensedItem(value, out license)) return license;

					return null;
				}
				public bool IsLicensedItem(string value, out RusterLicensedItem license)
				{
					license = null;

					foreach (var userPicture in Items)
					{
						if (userPicture.Value == value)
						{
							license = userPicture;
							return true;
						}
					}

					return false;
				}
				public bool IsLicensedItem(string imageUrl)
				{
					var userPicture = (RusterLicensedItem)null;
					return IsLicensedItem(imageUrl, out userPicture);
				}
			}
			public class LotteryConfig
			{
				[JsonProperty("EventDuration (in seconds)")]
				public float EventDuration { get; set; } = 60f * 3f;
				public int MinimumTickets { get; set; } = 5;
				public int TaxThreshold { get; set; } = 5000;
				public int MinimumPlayers { get; set; } = 2;
			}
			public class TradeConfig
			{
				public int TradingPrice { get; set; } = 20;
			}
		}

		[Serializable]
		public class RootData
		{
			public StockData Stock { get; set; } = new StockData();
			public List<RusterUser> Users { get; set; } = new List<RusterUser>();
			public List<RusterFriendRequest> FriendRequests { get; set; } = new List<RusterFriendRequest>();
			public List<RusterFeed> Feeds { get; set; } = new List<RusterFeed>();
			public List<RusterConversation> Conversations { get; set; } = new List<RusterConversation>();
			public List<RusterStory> Stories { get; set; } = new List<RusterStory>();
			public Dictionary<ulong, List<RusterTransaction>> Purchases { get; set; } = new Dictionary<ulong, List<RusterTransaction>>();
			public Dictionary<ulong, List<RusterTransaction>> Sales { get; set; } = new Dictionary<ulong, List<RusterTransaction>>();
			public Dictionary<ulong, List<string>> LicensedItems { get; set; } = new Dictionary<ulong, List<string>>();
			public Dictionary<ulong, List<RusterMarketplaceListing>> GiftBaskets { get; set; } = new Dictionary<ulong, List<RusterMarketplaceListing>>();

			[JsonIgnore] public RusterUser Empty { get; } = new RusterUser() { Id = 0, CurrentDisplayName = "Unknown" };
			public RusterUser GetUser(ulong steamId)
			{
				var account = Users.FirstOrDefault(x => x?.Id == steamId);
				if (account == null)
				{
					account = new RusterUser { Id = steamId };
					account.Configuration.Language = Instance.Config.Localisation.DefaultLanguage;
					account.Refresh();
					Users.Add(account);
				}
				return account;
			}
			public RusterUser GetUser(ulong steamId, RusterServerViewer serverViewer)
			{
				if (serverViewer.CurrentServer == null) return GetUser(steamId);

				var account = serverViewer.Users.FirstOrDefault(x => x?.Id == steamId);
				if (account == null)
				{
					account = GetUser(steamId);
				}
				return account;
			}
			public RusterUser GetUser(BasePlayer player)
			{
				return GetUser(player.userID);
			}
			public bool UserExists(ulong steamId)
			{
				return Users.Any(x => x.Id == steamId);
			}

			public bool SendFriendRequest(RusterUser sentBy, RusterUser sentTo)
			{
				if (FriendRequests.Exists(x => x.SentById == sentBy.Id && x.SentToId == sentTo.Id) ||
					FriendRequests.Exists(x => x.SentById == sentTo.Id && x.SentToId == sentBy.Id)) return false;

				FriendRequests.Add(new RusterFriendRequest(sentBy, sentTo));
				return true;
			}
			public bool CancelFriendRequest(RusterUser sentBy, RusterUser sentTo)
			{
				FriendRequests.RemoveAll(x => x.SentById == sentBy.Id && x.SentToId == sentTo.Id);
				FriendRequests.RemoveAll(x => x.SentById == sentTo.Id && x.SentToId == sentBy.Id);
				return true;
			}
			public void HandleFriendRequest(bool doAccept, RusterUser sentBy, RusterUser sentTo)
			{
				if (doAccept)
				{
					if (!sentBy.Friends.Contains(sentTo.Id)) sentBy.Friends.Add(sentTo.Id);
					if (!sentTo.Friends.Contains(sentBy.Id)) sentTo.Friends.Add(sentBy.Id);
				}

				FriendRequests.RemoveAll(x => x.SentById == sentTo.Id && x.SentToId == sentBy.Id);
				FriendRequests.RemoveAll(x => x.SentById == sentBy.Id && x.SentToId == sentTo.Id);
			}
			public void RemoveFriend(RusterUser sentBy, RusterUser sentTo)
			{
				if (sentBy.Friends.Contains(sentTo.Id)) sentBy.Friends.RemoveAll(x => x == sentTo.Id);
				if (sentTo.Friends.Contains(sentBy.Id)) sentTo.Friends.RemoveAll(x => x == sentBy.Id);
			}

			public RusterFeed GetCommunityFeed()
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == CommunityFeedId);
				if (feed == null)
				{
					feed = new RusterFeed { Id = CommunityFeedId };
					Feeds.Insert(0, feed);
				}
				return feed;
			}
			public RusterFeed GetMarketplaceFeed()
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == MarketplaceFeedId);
				if (feed == null)
				{
					feed = new RusterFeed { Id = MarketplaceFeedId };
					Feeds.Insert(0, feed);
				}

				feed.Hashtags = new List<RusterHashtag>();
				foreach (var item in feed.Posts)
				{
					if (!item.IsMarketplaceListing()) continue;

					if (feed.Hashtags.Any(x => x.Filter == item.MarketplaceListing.Shortname && x.FilterType == RusterHashtag.FilterTypes.ItemShortname)) continue;
					feed.Hashtags.Add(
						new RusterHashtag(item.MarketplaceListing.Shortname, RusterHashtag.FilterTypes.ItemShortname)
						{
							Instances = feed.Posts.Count(x => x.IsMarketplaceListing() && x.MarketplaceListing.Shortname == item.MarketplaceListing.Shortname)
						});
				}

				return feed;
			}
			public RusterFeed GetUserStoreFeed()
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == UserStoreFeedId);
				if (feed == null)
				{
					feed = new RusterFeed { Id = UserStoreFeedId };
					Feeds.Insert(0, feed);
				}

				feed.Hashtags = new List<RusterHashtag>();
				foreach (var item in feed.Posts)
				{
					if (!item.IsMarketplaceListing()) continue;

					if (feed.Hashtags.Any(x => x.Filter == item.MarketplaceListing.Shortname && x.FilterType == RusterHashtag.FilterTypes.ItemShortname)) continue;
					feed.Hashtags.Add(
						new RusterHashtag(item.MarketplaceListing.Shortname, RusterHashtag.FilterTypes.ItemShortname)
						{
							Instances = feed.Posts.Count(x => x.IsMarketplaceListing() && x.MarketplaceListing.Shortname == item.MarketplaceListing.Shortname)
						});
				}

				return feed;
			}
			public RusterFeed GetLotteryStoreFeed()
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == LotteryStoreFeedId);
				if (feed == null)
				{
					feed = new RusterFeed { Id = LotteryStoreFeedId };
					Feeds.Insert(0, feed);
				}

				feed.Hashtags = new List<RusterHashtag>();
				foreach (var item in feed.Posts)
				{
					if (!item.IsMarketplaceListing()) continue;

					if (feed.Hashtags.Any(x => x.Filter == item.MarketplaceListing.Shortname && x.FilterType == RusterHashtag.FilterTypes.ItemShortname)) continue;
					feed.Hashtags.Add(
						new RusterHashtag(item.MarketplaceListing.Shortname, RusterHashtag.FilterTypes.ItemShortname)
						{
							Instances = feed.Posts.Count(x => x.IsMarketplaceListing() && x.MarketplaceListing.Shortname == item.MarketplaceListing.Shortname)
						});
				}

				return feed;
			}
			public RusterFeed GetBlackmarketFeed()
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == BlackmarketFeedId);
				if (feed == null)
				{
					feed = new RusterFeed { Id = BlackmarketFeedId };
					Feeds.Insert(0, feed);
				}

				return feed;
			}
			public RusterFeed GetRedRoomFeed()
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == RedRoomFeedId);
				if (feed == null)
				{
					feed = new RusterFeed { Id = RedRoomFeedId };
					Feeds.Insert(0, feed);
				}

				return feed;
			}

			public void RegisterGift(ulong userId, RusterMarketplaceListing listing, int amount = 1)
			{
				var savedListing = new RusterMarketplaceListing
				{
					CustomName = listing.CustomName,
					Shortname = listing.Shortname,
					Skin = listing.Skin,
					SkinIconUrl = listing.SkinIconUrl,
					Text = listing.Text,
					Amount = amount,
					SubEntityId = listing.SubEntityId,
					WholeStack = true
				};

				var browser = Instance.GetBrowser(userId);
				if (browser.GiftBasketContainer != null)
				{
					browser.GiftBasketContainer.Kill();
					browser.GiftBasketContainer = null;
				}

				if (!GiftBaskets.ContainsKey(userId))
				{
					GiftBaskets.Add(userId, new List<RusterMarketplaceListing>() { savedListing });
				}
				else
				{
					GiftBaskets[userId].Insert(0, savedListing);
				}
			}
			public void RegisterGift(RusterUser user, RusterMarketplaceListing listing, int amount = 1)
			{
				RegisterGift(user.Id, listing, amount);
			}
			public List<RusterMarketplaceListing> GetGiftBasket(ulong userId)
			{
				if (!GiftBaskets.ContainsKey(userId))
				{
					var list = new List<RusterMarketplaceListing>();
					GiftBaskets.Add(userId, list);
					return list;
				}

				return GiftBaskets[userId];
			}
			public List<RusterMarketplaceListing> GetGiftBasket(RusterUser user)
			{
				return GetGiftBasket(user.Id);
			}

			public void RegisterPurchase(ulong userId, RusterTransaction transaction)
			{
				if (!Purchases.ContainsKey(userId))
				{
					Purchases.Add(userId, new List<RusterTransaction>() { transaction });
				}
				else
				{
					Purchases[userId].Insert(0, transaction);
				}
			}
			public void RegisterPurchase(RusterUser user, RusterTransaction transaction)
			{
				RegisterPurchase(user.Id, transaction);
			}
			public List<RusterTransaction> GetPurchases(ulong userId)
			{
				if (!Purchases.ContainsKey(userId))
				{
					var list = new List<RusterTransaction>();
					Purchases.Add(userId, list);
					return list;
				}

				return Purchases[userId];
			}
			public List<RusterTransaction> GetPurchases(RusterUser user)
			{
				return GetPurchases(user.Id);
			}

			public void RegisterSales(ulong userId, RusterTransaction transaction)
			{
				if (!Sales.ContainsKey(userId))
				{
					Sales.Add(userId, new List<RusterTransaction>() { transaction });
				}
				else
				{
					Sales[userId].Insert(0, transaction);
				}
			}
			public void RegisterSale(RusterUser user, RusterTransaction transaction)
			{
				RegisterSales(user.Id, transaction);
			}
			public List<RusterTransaction> GetSales(ulong userId)
			{
				if (!Sales.ContainsKey(userId))
				{
					var list = new List<RusterTransaction>();
					Sales.Add(userId, list);
					return list;
				}

				return Sales[userId];
			}
			public List<RusterTransaction> GetSales(RusterUser user)
			{
				return GetSales(user.Id);
			}

			public void RegisterLicensedItem(ulong userId, RusterLicensedItem item)
			{
				if (!LicensedItems.ContainsKey(userId))
				{
					LicensedItems.Add(userId, new List<string>() { item.Value });
				}
				else
				{
					LicensedItems[userId].Add(item.Value);
				}
			}
			public void RegisterLicensedItem(RusterUser user, RusterLicensedItem item)
			{
				RegisterLicensedItem(user.Id, item);
			}
			public List<string> GetLicensedItems(ulong userId)
			{
				if (!LicensedItems.ContainsKey(userId))
				{
					var list = new List<string>();
					LicensedItems.Add(userId, list);
					return list;
				}

				return LicensedItems[userId];
			}
			public List<string> GetLicensedItems(RusterUser user)
			{
				return GetLicensedItems(user.Id);
			}
			public bool HasLicense(ulong userId, string value)
			{
				foreach (var item in GetLicensedItems(userId))
				{
					if (item == value) return true;
				}

				return false;
			}
			public bool HasLicense(RusterUser user, string value)
			{
				return HasLicense(user.Id, value);
			}

			public RusterStory GetStory(int id)
			{
				return Stories.FirstOrDefault(x => x.Id == id);
			}
			public RusterStory GetStory(int id, RusterServerViewer serverViewer)
			{
				if (!serverViewer.IsViewing()) return GetStory(id);

				return serverViewer.Stories.FirstOrDefault(x => x.Id == id);
			}
			public RusterStory CreateStory(RusterUser user, Item photoItem)
			{
				var story = new RusterStory
				{
					UserId = user.Id,
					Id = RusterStory.GetId(),
					Ticks = DateTick.Current.Ticks
				};

				var photoEntities = BaseNetworkable.serverEntities.OfType<PhotoEntity>();
				var photoEntity = photoEntities.FirstOrDefault(x => x.net.ID == photoItem.instanceData.subEntity);
				var photoData = FileStorage.server.Get(photoEntity.ImageCrc, FileStorage.Type.jpg, photoEntity.net.ID);
				var folder = Instance.GetTempFolder();
				var file = $"{folder}{Path.DirectorySeparatorChar}{photoEntity.ImageCrc}_{photoEntity.net.ID}_{DateTime.Now.Ticks}.jpg";
				var temporaryFile = $"{folder}{Path.DirectorySeparatorChar}{photoEntity.ImageCrc}_{photoEntity.net.ID}_{DateTime.Now.Ticks}_temp.jpg";
				var exception = (Exception)null;

				OsEx.File.Create(file, photoData);
				var imgurUrl = global::RusterNET.Core.PhotoUpload.UploadImageToImgurWithWatermark(file, temporaryFile, Instance.Config.PhotographUpload.ImgurClientId,
					Instance.GetRusterWatermarkPath(), out exception,
					offsetY: 0,
					height: 0);

				story.PhotoUrl = imgurUrl;
				story.PhotoTag = Instance.Config.Profanity.DoProfanityCheck(photoItem.text);

				ServerMgr.Instance.Invoke(() =>
				{
					try
					{
						OsEx.File.Delete(file);
						OsEx.File.Delete(temporaryFile);
					}
					catch { }
				}, 3f);

				if (exception != null)
				{
					Instance.Puts(exception.ToString());
					return null;
				}

				Stories.Insert(0, story);
				return story;
			}
			public RusterStory CreateStory(RusterUser user, string photoUrl, string photoContent)
			{
				var story = new RusterStory
				{
					UserId = user.Id,
					Id = RusterStory.GetId(),
					Ticks = DateTick.Current.Ticks
				};

				story.PhotoUrl = photoUrl;
				story.PhotoTag = Instance.Config.Profanity.DoProfanityCheck(photoContent);

				Stories.Insert(0, story);
				return story;
			}
			public bool DeleteStory(int id)
			{
				var story = GetStory(id);
				if (story == null) return false;

				Stories.Remove(story);
				return true;
			}
			public RusterFeed GetFeed(ulong id)
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == id);
				if (feed == null)
				{
					feed = new RusterFeed { Id = id };
					Feeds.Add(feed);
				}

				if (string.IsNullOrEmpty(feed.BackgroundUrl) && feed.GetFeedType() == RusterFeed.FeedTypes.User)
					feed.BackgroundUrl = RusterBrowser.UserFeedBackgroundUrl;

				return feed;
			}
			public RusterFeed GetFeed(RusterUser user)
			{
				return GetFeed(user.Id);
			}
			public RusterFeed GetFeed(RusterFeed.RusterPost post, string customFeedTitle = null)
			{
				var feed = Feeds.FirstOrDefault(x => x.Id == (ulong)post.Id);
				if (feed == null)
				{
					feed = new RusterFeed { Id = (ulong)Mathf.Abs(post.Id) };
					Feeds.Add(feed);
				}

				if (!string.IsNullOrEmpty(customFeedTitle)) feed.Title = customFeedTitle;
				if (feed.GetFeedType() == RusterFeed.FeedTypes.Post && string.IsNullOrEmpty(feed.BackgroundUrl)) feed.BackgroundUrl = RusterBrowser.PostBackgroundUrl;
				return feed;
			}
			public RusterFeed.RusterPost GetPost(int id, RusterUser observer = null)
			{
				var advertPost = id == 900 ? GetAdvertsNoticePost(true, observer) : id == 901 ? GetAdvertsNoticePost(false, observer) : null;
				if (advertPost != null && advertPost.Id == id) return advertPost;

				var flipbookPost = id == 902 ? GetFlipbookAdvertNoticePost(0, observer) : id == 903 ? GetFlipbookAdvertNoticePost(1, observer) : id == 904 ? GetFlipbookAdvertNoticePost(2, observer) : null;
				if (flipbookPost != null && flipbookPost.Id == id) return flipbookPost;

				foreach (var feed in Feeds)
				{
					foreach (var post in feed.Posts)
					{
						if (post.Id == id) return post;
					}
				}

				return null;
			}
			public RusterFeed.RusterPost GetPost(int id, out RusterFeed postFeed, RusterUser observer = null)
			{
				var advertPost = GetAdvertsNoticePost(RandomEx.GetRandomInteger(0, 10) < 5, observer);
				var flipbookPost = id == 902 ? GetFlipbookAdvertNoticePost(0, observer) : id == 903 ? GetFlipbookAdvertNoticePost(1, observer) : id == 904 ? GetFlipbookAdvertNoticePost(2, observer) : null;
				postFeed = null;
				if (advertPost != null && advertPost.Id == id) return advertPost;
				if (flipbookPost != null && flipbookPost.Id == id) return flipbookPost;

				foreach (var feed in Feeds)
				{
					foreach (var post in feed.Posts)
					{
						if (post.Id == id)
						{
							postFeed = feed;
							return post;
						}
					}
				}

				return null;
			}
			public RusterConversation GetConversation(int id)
			{
				return Conversations.FirstOrDefault(x => x.Id == id);
			}
			public RusterConversation GetTeamConversation(RelationshipManager.PlayerTeam team)
			{
				if (team == null) return null;

				var id = 6666 + (int)team.teamID;
				var conversation = GetConversation(id);
				if (conversation == null)
				{
					conversation = new RusterConversation
					{
						Id = id,
						CanDelete = false,
						ConversationType = RusterConversation.ConversationTypes.Team
					};
					foreach (var member in team.members)
					{
						conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, member, "Joined the team!", true));
						conversation.ViewerList.Add(member);
						conversation.Users.Add(member);
					}

					Conversations.Insert(0, conversation);
				}

				foreach (var member in team.members)
				{
					if (!conversation.Users.Contains(member))
					{
						conversation.PostMessage(new RusterConversation.RusterDirectMessage(conversation, member, "Joined the team!", true));
						conversation.ViewerList.Add(member);
					}
				}

				conversation.Users.Clear();
				foreach (var member in team.members) conversation.Users.Add(member);

				return conversation;
			}
			public RusterConversation UpdateTeamConversation(RelationshipManager.PlayerTeam team)
			{
				var conversation = GetTeamConversation(team);
				if (team == null || team.members.Count <= 1)
				{
					Conversations.Remove(conversation);
					return null;
				}

				return conversation;
			}
			public void DeleteTeamConversation(RelationshipManager.PlayerTeam team)
			{
				if (team == null) return;

				var conversation = GetTeamConversation(team);
				if (conversation != null) Conversations.Remove(conversation);
			}
			public RusterConversation GetReportsGroup()
			{
				var conversation = GetConversation(ReportGroupId);

				if (conversation == null)
				{
					conversation = new RusterConversation
					{
						Id = ReportGroupId,
						CustomTitle = "Reports",
						ConversationType = RusterConversation.ConversationTypes.Group,
						CanDelete = false,
						CanManage = false
					};

					Conversations.Insert(0, conversation);
				}

				conversation.CanReact = false;
				conversation.IsLocked = true;

				return conversation;
			}

			public bool HasFriendRequestSent(RusterUser sentBy, RusterUser sentTo)
			{
				return FriendRequests.Exists(x => x.SentById == sentBy.Id && x.SentToId == sentTo.Id);
			}
			public void Block(RusterUser sentBy, RusterUser sentTo)
			{
				if (sentBy.IsFriends(sentTo.Id)) RemoveFriend(sentBy, sentTo);
				CancelFriendRequest(sentBy, sentTo);
				CancelFriendRequest(sentTo, sentBy);

				if (!sentBy.Blocked.Contains(sentTo.Id)) sentBy.Blocked.Add(sentTo.Id);
				else sentBy.Blocked.Remove(sentTo.Id);
			}

			public RusterUser GetAdvertsAccount()
			{
				var user = GetUser(100);
				user.CurrentDisplayName = "";
				user.CustomDisplayName = "Adverts";
				user.AvatarUrl = "https://oxidemod.org/assets/styles/oxide/logo.og.png";
				user.IsBot = true;

				return user;
			}
			public RusterUser GetLotteryAccount()
			{
				var user = GetUser(105);
				user.CurrentDisplayName = "Lottery Guy";
				user.CustomDisplayName = "Lottery";
				user.AboutMe = $"This account is managed and configured by the Ruster.NET orgaynisation. All opinions are ours.";
				user.AvatarUrl = "https://cdn.discordapp.com/attachments/844914604080889867/940625996816711680/andioof.png";
				user.IsBot = true;

				return user;
			}
			public RusterUser GetCodeflingNewsAccount()
			{
				var user = GetUser(101);
				user.CurrentDisplayName = "";
				user.CustomDisplayName = "Codefling News";
				user.AvatarUrl = "https://oxidemod.org/assets/styles/oxide/logo.og.png";
				user.IsBot = true;

				return user;
			}
			public RusterFeed.RusterPost GetAdvertsNoticePost(bool is24hAdvert, RusterUser observer)
			{
				var message = "";

				if (observer == null) message = is24hAdvert ? "Buy an advert which lasts for 24 hours now!" : "Buy an advert which lasts for a week now!";
				else message = GetPhrase(is24hAdvert ? $"24hadv_variation{RandomEx.GetRandomInteger(1, 3)}" : $"1wadv_variation{RandomEx.GetRandomInteger(1, 3)}", observer.Id);

				var post = RusterFeed.RusterPost.Create(GetAdvertsAccount(), message);
				post.Id = is24hAdvert ? 900 : 901;
				post.Location.Name = "you";
				post.Location.Position = new RusterFeed.RusterPost.RusterVector3(Vector3.one);
				post.Advert = new RusterFeed.RusterPost.RusterAdvert { DurationHours = is24hAdvert ? 24 : 24 * Instance.Config.Ads.AdvertPriceMultiplier1w };
				post.CanRate = false;
				post.MarketplaceListing = new RusterMarketplaceListing
				{
					Shortname = "paper",
					IsPurchased = false,
					Skin = is24hAdvert ? Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinId), RusterMarketplace24hAdvertSkinId) : Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinId), RusterMarketplace1wAdvertSkinId),
					Price = is24hAdvert ? Instance.Config.Ads.AdvertPrice24h : (int)(Instance.Config.Ads.AdvertPrice24h * Instance.Config.Ads.AdvertPriceMultiplier1w),
					Amount = 1,
					CustomName = is24hAdvert ? Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinName), RusterMarketplace24hAdvertSkinName) : Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinName), RusterMarketplace1wAdvertSkinName),
					Coupon = is24hAdvert ? Instance.Config.Advert.Advert24hCoupon : Instance.Config.Advert.Advert1wCoupon
				};

				return post;
			}
			public RusterFeed.RusterPost GetFlipbookAdvertNoticePost(int type, RusterUser observer)
			{
				var message = "";

				message = "Create a short-film with your friends and watch it play back as a GIF!";

				var post = RusterFeed.RusterPost.Create(GetAdvertsAccount(), message);
				post.Id = 902 + type;
				post.Location.Name = "you";
				post.Location.Position = new RusterFeed.RusterPost.RusterVector3(Vector3.one);
				post.Advert = new RusterFeed.RusterPost.RusterAdvert { DurationHours = 24 };
				post.CanRate = false;
				post.MarketplaceListing = new RusterMarketplaceListing
				{
					Shortname = "tool.instant_camera",
					IsPurchased = false,
					Skin = type == 0 ? RusterShortFlipbookSkinId : type == 1 ? RusterMediumFlipbookSkinId : RusterLongFlipbookSkinId,
					Price = type == 0 ? Instance.Config.Ads.AdvertShortFlipbookPrice : type == 1 ? Instance.Config.Ads.AdvertMediumFlipbookPrice : Instance.Config.Ads.AdvertLongFlipbookPrice,
					Amount = 1,
					CustomName = type == 0 ? RusterShortFlipbookSkinName : type == 1 ? RusterMediumFlipbookSkinName : RusterLongFlipbookSkinName,
					Coupon = type == 0 ? Instance.Config.Advert.AdvertShortFlipbookCoupon : type == 1 ? Instance.Config.Advert.AdvertMediumFlipbookCoupon : Instance.Config.Advert.AdvertLongFlipbookCoupon
				};

				return post;
			}
			public RusterFeed.RusterPost GetLotteryTicketNoticePost(int type, RusterUser observer)
			{
				var message = "";

				message = "Purchase a Lottery Ticket today to try your luck at earning tax returns!";

				var post = RusterFeed.RusterPost.Create(GetLotteryAccount(), message);
				post.Id = 910 + type;
				post.Location.Name = "you";
				post.Location.Position = new RusterFeed.RusterPost.RusterVector3(Vector3.one);
				post.Advert = new RusterFeed.RusterPost.RusterAdvert { DurationHours = 24 };
				post.CanRate = false;
				post.MarketplaceListing = new RusterMarketplaceListing
				{
					Shortname = "paper",
					IsPurchased = false,
					Skin = type == 0 ? RusterLegitLotteryTicketSkinId : type == 1 ? RusterLuckyCharmLotteryTicketSkinId : RusterIconicLotteryTicketSkinId,
					Price = type == 0 ? Instance.Config.Ads.AdvertLegitLotteryTicketPrice : type == 1 ? Instance.Config.Ads.AdvertLuckyCharmLotteryTicketPrice : Instance.Config.Ads.AdvertIconicLotteryTicketPrice,
					Amount = 1,
					CustomName = type == 0 ? RusterLegitLotteryTicketSkinName : type == 1 ? RusterLuckyCharmLotteryTicketSkinName : RusterIconicLotteryTicketSkinName
				};

				return post;
			}
			public RusterFeed.RusterPost GetGiftCardNoticePost(int amount, RusterUser observer)
			{
				var message = "";

				message = "Buy a Gift Card and share it to people you're considerate about!";

				var post = RusterFeed.RusterPost.Create(GetAdvertsAccount(), message);
				post.Id = 930 + amount;
				post.Location.Name = "you";
				post.Location.Position = new RusterFeed.RusterPost.RusterVector3(Vector3.one);
				post.Advert = new RusterFeed.RusterPost.RusterAdvert { DurationHours = 24 };
				post.CanRate = false;
				post.MarketplaceListing = new RusterMarketplaceListing
				{
					Shortname = "paper",
					IsPurchased = false,
					Skin = RusterGiftCardSkinId,
					Price = amount,
					Amount = 1,
					CustomName = string.Format(RusterGiftCardSkinName, amount == 0 ? "Custom" : Instance.Config.Currency.GetValueName(Instance.GetBrowser(GetAdvertsAccount()), amount)),
					Text = JsonConvert.SerializeObject(new RusterGiftCard(observer, amount))
				};

				return post;
			}
			public RusterFeed.RusterPost GetRandomAdvert(RusterUser observer = null)
			{
				var defaultAdvert = RandomEx.GetRandomInteger(0, 10) <= 1;
				if (defaultAdvert) return GetAdvertsNoticePost(RandomEx.GetRandomInteger(0, 10) < 5, observer);

				var userAdverts = Facepunch.Pool.GetList<RusterFeed.RusterPost>();
				foreach (var user in Users) userAdverts.AddRange(user.GetAdvertPosts(true));

				var post = userAdverts[RandomEx.GetRandomInteger(0, userAdverts.Count - 1)];
				Facepunch.Pool.FreeList(ref userAdverts);
				return post;
			}

			public void Fixups()
			{
				foreach (var conversation in Conversations)
				{
					if (conversation.Id == 0) conversation.Id = RusterConversation.GetId();
				}

				Conversations.RemoveAll(x => x.Id != ReportGroupId && x.ViewerList.Count == 0);
				Users.RemoveAll(x => string.IsNullOrEmpty(x.CustomDisplayName) && string.IsNullOrEmpty(x.CurrentDisplayName));
			}
			public void UpdateItems(Item[] items, RusterUser observer = null)
			{
				foreach (var item in items)
				{
					UpdateItem(item, observer);
				}
			}
			public void UpdateItem(Item item, RusterUser observer = null)
			{
				if (item.skin == Instance.GetProConfig(nameof(RusterSkinId), RusterSkinId)) { item.name = Instance.GetProConfig(nameof(RusterSkinName), RusterSkinName); }
				else if (item.skin == Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinId), RusterMarketplace24hAdvertSkinId)) { item.name = Instance.GetProConfig(nameof(RusterMarketplace24hAdvertSkinName), RusterMarketplace24hAdvertSkinName); }
				else if (item.skin == Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinId), RusterMarketplace1wAdvertSkinId)) { item.name = Instance.GetProConfig(nameof(RusterMarketplace1wAdvertSkinName), RusterMarketplace1wAdvertSkinName); }
				else if (item.skin == Instance.GetProConfig(nameof(RusterBusinessCardSkinId), RusterBusinessCardSkinId))
				{
					if (string.IsNullOrEmpty(item.text)) return;

					var card = JsonConvert.DeserializeObject<RusterBusinessCard>(item.text);
					if (card != null)
					{
						var user = card.GetUser();
						item.name = string.Format(Instance.GetProConfig(nameof(RusterBusinessCardSkinName), RusterBusinessCardSkinName), user.GetDisplayName(false));
					}
				}
				else if (item.skin == RusterShortFlipbookSkinId || item.skin == RusterMediumFlipbookSkinId || item.skin == RusterLongFlipbookSkinId)
				{
					var type = 0;

					switch (item.skin)
					{
						case RusterShortFlipbookSkinId:
							type = 0;
							break;

						case RusterMediumFlipbookSkinId:
							type = 1;
							break;

						case RusterLongFlipbookSkinId:
							type = 2;
							break;
					}

					if (string.IsNullOrEmpty(item.text))
					{
						item.text = JsonConvert.SerializeObject(new RusterFlipbook(type == 0 ? 20 : type == 1 ? 40 : 180));
					}

					var flipbook = JsonConvert.DeserializeObject<RusterFlipbook>(item.text);
					if (flipbook != null)
					{
						item.name = $"{(type == 0 ? RusterShortFlipbookSkinName : type == 1 ? RusterMediumFlipbookSkinName : RusterLongFlipbookSkinName)} ({flipbook.Frames:n0}/{flipbook.MaximumFrames:n0})";
					}
				}
				else if (item.skin == RusterLegitLotteryTicketSkinId || item.skin == RusterLuckyCharmLotteryTicketSkinId || item.skin == RusterIconicLotteryTicketSkinId)
				{
					var type = 0;

					switch (item.skin)
					{
						case RusterLegitLotteryTicketSkinId:
							type = 0;
							break;

						case RusterLuckyCharmLotteryTicketSkinId:
							type = 1;
							break;

						case RusterIconicLotteryTicketSkinId:
							type = 2;
							break;
					}

					item.name = $"{(type == 0 ? RusterLegitLotteryTicketSkinName : type == 1 ? RusterLuckyCharmLotteryTicketSkinName : RusterIconicLotteryTicketSkinName)}";
				}
				else if (item.skin == RusterGiftCardSkinId)
				{
					if (string.IsNullOrEmpty(item.text))
					{
						item.text = JsonConvert.SerializeObject(new RusterGiftCard(observer, 50));
					}

					var giftCard = JsonConvert.DeserializeObject<RusterGiftCard>(item.text);
					if (giftCard != null)
					{
						item.name = string.Format(RusterGiftCardSkinName, Instance.Config.Currency.GetValueName(Instance.GetBrowser(observer), giftCard.Value));
						item.MarkDirty();
					}
				}
			}

			public class StockData
			{
				[JsonProperty("Value (wipe 2/8/22)")]
				public int Value { get; set; }
			}
		}

		#endregion
	}
}
