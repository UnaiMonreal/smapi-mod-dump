/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/FerMod/StardewMods
**
*************************************************/


using System;
using System.Reflection;
using Harmony;
using MultiplayerEmotes.Extensions;
using StardewModdingAPI;
using StardewValley;

namespace MultiplayerEmotes.Framework.Patches {

	internal static class FarmerPatch {

		internal class DoEmotePatch : ClassPatch {

			public override MethodInfo Original { get; } = AccessTools.Method(typeof(Farmer), nameof(Farmer.doEmote), new[] { typeof(int) });
			public override MethodInfo Postfix { get; } = AccessTools.Method(typeof(DoEmotePatch), nameof(DoEmotePatch.DoEmote_Postfix));

			private static IReflectionHelper Reflection { get; set; }

			public static DoEmotePatch Instance { get; } = new DoEmotePatch();

			// Explicit static constructor to avoid the compiler to mark type as beforefieldinit
			static DoEmotePatch() { }

			private DoEmotePatch() { }

			public static DoEmotePatch CreatePatch(IReflectionHelper reflection) {
				Reflection = reflection;
				return Instance;
			}

			private static void DoEmote_Postfix(Farmer __instance, int whichEmote) {

#if DEBUG
				ModEntry.ModMonitor.Log($"DoEmote_Postfix (enabled: {Instance.PostfixEnabled})", LogLevel.Trace);
#endif
				if(!Instance.PostfixEnabled) {
					return;
				}

#if DEBUG
				ModEntry.ModMonitor.Log($"Farmer emote ({__instance.GetType()})", LogLevel.Trace);
#endif
				if (!Context.IsMultiplayer || !__instance.IsLocalPlayer) {
					return;
				}
#if DEBUG
				ModEntry.ModMonitor.Log("Farmer broadcasting emote.", LogLevel.Trace);
#endif
				// Traverse.Create(typeof(Game1)).Field("multiplayer").GetValue<Multiplayer>().BroadcastEmote(whichEmote);
				Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue().BroadcastEmote(whichEmote, __instance);

			}

		}

	}

}
