# In-game design factors behind the popularity of survivors-likes (research notes, 2026-10-13)

Scope: in-game elements only. Coverage is limited: about 17 tool calls, mostly secondary sources (Wikipedia, press coverage, one marketing case study). No GDC talk or full postmortem was found for VS, Brotato, Halls of Torment or Megabonk. Sales figures from VGI, Raijin or Gamalytic are modeled estimates, not official numbers.

## 1. Core loop, controls, session length, difficulty and power fantasy in the hit titles

### Takeaway
The hits share four traits: a very low skill floor (move only, auto-attack), short fixed-length runs (15-30 minutes, or under 30 minutes split into 20-90 second waves), screen-filling escalation, and one clear twist on the formula. Examples of the twist are Brotato's shop between waves, the manual aim and reload in 20 Minutes Till Dawn, mining in DRG: Survivor, and 3D terrain with jumping in Megabonk.

### Cited Findings
- **Vampire Survivors (VS)**
  - The character attacks automatically, so the player only moves. Reviewers called it a "Single-Stick Masterpiece". — [Wikipedia: Vampire Survivors](https://en.wikipedia.org/wiki/Vampire_Survivors)
  - Each stage has a soft time limit of 15, 20 or 30 minutes. Reaching it counts as a successful completion and pays bonus gold. — [Wikipedia](https://en.wikipedia.org/wiki/Vampire_Survivors)
  - The game was inspired by Magic Survival, a 2019 mobile game where the character also auto-attacks. — [Wikipedia](https://en.wikipedia.org/wiki/Vampire_Survivors)
  - Galante (Barclays interview) took "the simplest gameplay he could think of", a single-stick shooter with powered-up spells. He says the result was organic, not planned. — [Barclays Games](https://games.barclays/resource-hub/games/industry-insights/how-vampire-survivors-became-a-hit-when-creator-poncle-was-ready-to-give-up)
  - Polygon noted the game's depth: no streamer it watched played the same way. — [Wikipedia (Reception)](https://en.wikipedia.org/wiki/Vampire_Survivors)
  - Concurrent players on Steam passed 30,000 in late January 2022 and 70,000 in February 2022. — [Wikipedia](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **Brotato**
  - Shooting is automatic by default, and players can switch to manual aim. — [Wikipedia: Brotato](https://en.wikipedia.org/wiki/Brotato)
  - A run takes under 30 minutes. Waves last 20 to 90 seconds, and there is a shop after each wave. At 1.0 it had 40+ characters and hundreds of items and weapons. — [Brotato 1.0 press release, Game Developer, 2023-06-23](https://www.gamedeveloper.com/press-release/brotato-10-release)
  - Developer intent: "appeal to a very specific niche ... by combining mechanics from a few different genres" (survivors plus an autochess-style shop), "glued together by a hand drawn artstyle and a potato theme". — [Game Developer press release](https://www.gamedeveloper.com/press-release/brotato-10-release)
  - Critics called it "addictive, quick, and accessible" (Screen Rant) and "a smart evolution of the formula" with dynamic character builds (Digital Trends). Rock Paper Shotgun (during early access) said deaths came too easily. — [Wikipedia: Brotato](https://en.wikipedia.org/wiki/Brotato)
- **20 Minutes Till Dawn (20MTD)**
  - The developer, flanne, removed auto-fire, saying he "honestly disliked the Vampire Survivors auto-shoot gameplay". Players aim and reload. The goal was mechanical depth in short, casual sessions. — [How To Market A Game, 2022-06-14](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
  - Feedback from r/playmygame and itch.io asked for more build variety, more enemy variety and more difficulty, and he added content in those directions. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
  - The demo's median playtime was about 1 hour 15 minutes. The author puts the median indie demo at about 12 minutes. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
  - The Japanese developer interview is headlined around delivering the many "power fantasies" players imagine. Only the headline could be checked. — [GameBusiness.jp, 2022-06-15](https://www.gamebusiness.jp/article/2022/06/15/20252.html)
- **Megabonk (2025 hit)**
  - Third-person 3D with procedural maps and verticality. It adds sliding, jumping and speed boosts, has 20 unlockable characters, 70+ items and 200+ quests, and only 2 maps. It is single-player. — [Wikipedia: Megabonk](https://en.wikipedia.org/wiki/Megabonk)
  - It sold over 1 million copies in two weeks and peaked at 117,336 concurrent players (SteamDB). It released on 2025-09-18 at $5.49. — [Wikipedia](https://en.wikipedia.org/wiki/Megabonk); [Notebookcheck](https://www.notebookcheck.net/Vampire-Survivors-esque-indie-Reverse-Bullet-Hell-Game-Megabonk-Sells-1-Million-Copies-in-Just-Two-Weeks.1132354.0.html)
  - Praised for: 3D terrain and verticality, satisfying build-crafting, an addictive loop. Criticized for: repetitive challenges, difficulty spikes, "memey" humor and limited scenery. — [Wikipedia](https://en.wikipedia.org/wiki/Megabonk)
  - VGI estimates that 62.5% of Megabonk players also own VS, meaning it drew on the existing genre audience. — [Notebookcheck](https://www.notebookcheck.net/Megabonk-beats-Hades-2-s-all-time-record-as-it-morphs-into-Steam-s-new-indie-sensation.1136297.0.html)
- **Deep Rock Galactic: Survivor**
  - Early access began 2024-02-14. It sold 500k in the first week and 1M within a month, with a launch peak of about 57k concurrent players. Steam reviews were around 83-84% positive. — [Game World Observer](https://gameworldobserver.com/2024/02/22/deep-rock-galactic-survivor-sales-500k-copies-first-week); [Game Developer](https://gamasutra.com/business/deep-rock-galactic-survivor-grows-to-sell-1-million-early-access-copies)
- **Halls of Torment**
  - Sold 70,000+ units in its first week of early access (May 2023) and had 1M+ players by its first anniversary. Full release was 2024-09-24. — [Wikipedia: Halls of Torment](https://en.wikipedia.org/wiki/Halls_of_Torment); [Pieuvre](https://www.pieuvre.ca/?p=62650)
  - The developer (Chasing Carrots) "reduced our scope drastically" after a 5-year project. They cite VS as the biggest influence and 20MTD as the trigger for entering the genre. — [FullCleared interview](https://fullcleared.com/features/inside-halls-of-torment-an-interview-with-chasing-carrots/)
- **HoloCure**
  - A free fan game. It reached 99% positive reviews and 45,000 concurrent players at its Steam launch. Rock Paper Shotgun called it "a much slower burn when it comes to fulfilling the power fantasy". — [search summary of OpenCritic, Game8 and RPS](https://opencritic.com/game/13312/holocure/reviews)
- **Soulstone Survivors**
  - Steam reviews are about 91% positive across about 26.6k reviews. Raijin estimates about 691k copies and about $6.2M gross (medium confidence, as of July 2026). Reviewers praised its depth and meta-progression and criticized "characters feeling too similar". — [Raijin](https://raijin.gg/app/2066020/Soulstone_Survivors); [Pure Xbox, 2025-06](https://www.purexbox.com/news/2025/06/soulstone-survivors-is-out-now-on-xbox-and-some-say-its-better-than-vampire-survivors). The Raijin all-time peak of 3,808 looks low and should be checked on SteamDB.
- **Genre definition:** Players fight successive waves while moving, pick periodic attack and passive upgrades, and each run is roguelike. In the "bullet heaven" variant the player's projectiles fill the screen, which is the core of the power fantasy. — [Wikipedia: VS-like](https://en.wikipedia.org/wiki/Vampire_Survivors%E2%80%93like); [Exploring The Games, 2024-07-03](https://whatsinkedinmymind.substack.com/p/exploring-the-survivors-like-or-bullet-heavens)

### Inferences
- The hits keep runs to 30 minutes or less, and Brotato splits that into 20-90 second waves with natural break points. If Soul Saver's runs or endless mode go well past 30 minutes before a "clear", a short clear target would likely help. Halfway checkpoints such as boss arena kills can also serve as break points.
- One clear, describable twist per game shows up in every breakout. Soul Saver's candidates are boss barrier arenas, weapon evolution on boss kills and the Soul Tree, and one of them should be the single line the game is described by.
- A manual-aim option (Brotato's toggle, 20MTD's aiming) widens the audience without losing casual players, as long as auto-attack stays the default.

### Gaps
- No primary source gave average session length, D1 or D7 retention, or difficulty-curve data for any of these titles.
- No developer statements were found for Death Must Die or Magicraft. They were not researched in depth.

## 2. Meta progression and unlock design (achievements as unlocks, secrets, evolutions, codex)

### Takeaway
In VS, unlocks double as the to-do list: most content unlocks through in-run achievements or gold, and each new unlock is a reason for another run. Weapon evolution recipes and secrets create a discovery-and-share loop, which press credits as part of the game's addictiveness and "no two streamers play the same" depth.

### Cited Findings
- **VS unlock list.** Characters, stages and weapons unlock through gameplay achievements or in-game currency. Gold earned in runs is spent between runs on permanent stat upgrades. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors); [The Conversation](https://theconversation.com/vampire-survivors-how-developers-used-gambling-psychology-to-create-a-bafta-winning-game-203613)
- **Near-goals keep players going.** The Conversation describes "a lengthy list" of achievements that pushes players to try other characters and strategies. It also describes near-misses ("almost reached 30 minutes", "one more chest") as slot-machine-style "just one more go" design, based on Galante's gambling-industry background. — [The Conversation](https://theconversation.com/vampire-survivors-how-developers-used-gambling-psychology-to-create-a-bafta-winning-game-203613)
- **VS evolutions.** Most weapons have an evolved form. It unlocks when the player opens a chest after fully upgrading the weapon and meeting other conditions, usually holding a specific passive item. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **Megabonk** uses 200+ quests as its unlock list, plus "Silver" meta currency earned from challenges, silver pots and the Silver Tome. Critics credit "a plethora of weapons, tomes, items, and unlockables" with keeping players hooked, but GamesHub faulted its "repetitive challenges". — [Wikipedia: Megabonk](https://en.wikipedia.org/wiki/Megabonk); [Notebookcheck](https://www.notebookcheck.net/Vampire-Survivors-esque-indie-Reverse-Bullet-Hell-Game-Megabonk-Sells-1-Million-Copies-in-Just-Two-Weeks.1132354.0.html)
- **Soulstone Survivors**' meta-progression was praised in early-access reviews. — [Pure Xbox](https://www.purexbox.com/news/2025/06/soulstone-survivors-is-out-now-on-xbox-and-some-say-its-better-than-vampire-survivors)
- **Brotato** has 40+ characters, many unlocked through progression, and each starts with different attributes. Characters act as rule-changing modifiers rather than reskins. — [Wikipedia: Brotato](https://en.wikipedia.org/wiki/Brotato); [press release](https://www.gamedeveloper.com/press-release/brotato-10-release)
- **Long-term content updates** won VS the BAFTA for Evolving Game in 2025. Galante called a sequel "unlikely" because the existing design lets the team keep adding content and mechanics. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors); [80.lv](https://80.lv/articles/vampire-survivors-dev-says-the-sequel-to-the-game-is-unlikely/)

### Inferences
- Soul Saver's 100+ node Soul Tree is a gold-sink style system like VS PowerUps. On top of it, an explicit list of unlock conditions ("do X to unlock character Y") gives each run a near goal. Achievement-gated heroes and evolutions seen as "???" would add the discovery hook.
- Players share and discuss evolution recipes. Showing 37 evolutions as a codex with hidden entries supports that discovery and word of mouth.
- Characters need to feel mechanically distinct. "Characters too similar" was Soulstone's notable negative.

### Gaps
- No quantitative data was found on how unlock or secret design affects retention or reviews.
- No primary source was found on how widely secret characters were shared, such as in Reddit threads.

## 3. Visual and audio juice, and readability

### Takeaway
VS's signature "juice" is the chest moment: the game pauses, a jingle plays and colors burst, borrowed from slot machines. Its biggest readability advantage, per critics, is that the player stays distinguishable amid the clutter, which most clones failed at.

### Cited Findings
- **The VS chest moment.** Galante drew on his gambling-industry knowledge of flashy slot-machine graphics for the chest-opening animation. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors)
  - Touching a chest pauses the game, plays a celebratory tune and bursts colors. Chests hold 1, 3 or 5 items, and a review singles out the jingle on 3 or 5 rewards as a standout. — [The Vibes review](https://www.thevibes.com/articles/lifestyles/53373/vampire-survivors-an-escalating-dopamine-rush-disguised-as-a-retro-game); [Wikipedia](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **Dopamine framing.** Kotaku and Rock Paper Shotgun compared its addictive appeal to dopamine. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **Readability as a differentiator.** Ed Thorn (Rock Paper Shotgun) wrote that most clones did a poor job of distinguishing the player from the visual clutter compared with the original. — [Wikipedia: VS-like](https://en.wikipedia.org/wiki/Vampire_Survivors%E2%80%93like)
  - A Pocket Gamer piece notes that the genre is known for blinding effects and that 3D versions raise performance concerns. — [Pocket Gamer: Twilight Survivors](https://www.pocketgamer.com/twilight-survivors/out-now)
  - One blog review found that a minimalist black background aided clarity, but too little detail made enemy types hard to tell apart. — [gabtoschi review](https://gabtoschi.com/blog/review/geometry-survivor.html)
- **Audio.** VS's music was first made with simple digital tools and later re-recorded with a full orchestra, and the orchestral recording was itself promoted. — [80.lv](https://80.lv/articles/vampire-survivors-dropped-videos-of-orchestra-soundtrack-recordings/)
- **Art style as appeal.** Galante said the Castlevania-inspired art was a big reason he kept going. — [Barclays](https://games.barclays/resource-hub/games/industry-insights/how-vampire-survivors-became-a-hit-when-creator-poncle-was-ready-to-give-up)
  - 20MTD's art was judged more attractive than competing clones, and its anime style was partly chosen to appeal to Asian audiences. Chinese player numbers for its prologue were strong. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)

### Inferences
- **Chest moments.** Soul Saver's boss kills that trigger weapon evolution are natural "chest moments". They should get the full treatment: pause, a rising jingle, light bursts, and staged reveals by rarity.
- **Effect readability.** Keep the player sprite readable at all times (an outline or shadow ring, effects drawn below the player). Effect opacity or density options are commonly requested, but no source in this pass confirmed which games ship them.

### Gaps
- No sourced data was found on damage-number design, on effect-opacity options in specific games, or on any measured effect of juice on reviews.

## 4. Co-op, Steam Deck and controller support, accessibility, low specs

### Takeaway
Co-op came late to the hits: Brotato added local co-op in October 2024, about 2 years after early access, and VS added local co-op in 2023 and online co-op in October 2025. Both sold millions single-player first, and 2025's biggest breakout (Megabonk) shipped single-player only. Steam Deck performance was explicitly praised for VS and Brotato.

### Cited Findings
- **VS co-op.** Local co-op for up to 4 players arrived with the Switch version in 2023. Online co-op went into beta in August 2025 and fully shipped in the October 2025 update. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **Brotato co-op.** Local co-op shipped in October 2024 with the Abyssal Terrors DLC. Brotato was also nominated for Best Steam Deck Game at the 2023 Steam Awards. — [Wikipedia: Brotato](https://en.wikipedia.org/wiki/Brotato)
- **Megabonk** is single-player and sold 1M in two weeks. — [Wikipedia: Megabonk](https://en.wikipedia.org/wiki/Megabonk)
- **Steam Deck.** GamesIndustry.biz made VS its 2022 Game of the Year, highlighting its excellent performance on the Steam Deck. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **Accessibility options.** Flexible difficulty options, modifiers and a game-speed slider were praised as standout features in one clone roundup. — [search summary of PC Gamer, 2022 roundup](https://www.pcgamer.com/so-so-many-vampire-survivors-clones-came-out-this-year/) (the article body could not be fetched, so this is unverified)
- **Platform spread.** Brotato went from Steam EA (2022-09) to mobile (2023-03), Switch (2023-08) and PS/Xbox (2024-01), reaching 10M+ total sales by 2025. — [Wikipedia: Brotato](https://en.wikipedia.org/wiki/Brotato)

### Inferences
- Co-op is not required for breakout success, but it is a common long-tail addition. Controller support and Deck verification matter more early on, given the Deck praise for VS and Brotato.

### Gaps
- No measured uplift from Deck Verified status, co-op or low system requirements was found.

## 5. Price point and impulse purchase

### Takeaway
The breakout hits launched at about $3-5.49: VS around $3-5, 20MTD at $2.99, Brotato at $5, Megabonk at $5.49. The 20MTD case study argues that the low price turned wishlisting into an immediate impulse buy.

### Cited Findings
- **VS** shipped at a very low price (press headlines say $3-4), and Wikipedia's reception section credits the low price with helping it reach a wide audience. — [Wikipedia: VS](https://en.wikipedia.org/wiki/Vampire_Survivors)
- **20MTD**
  - Launched at $2.99 with no launch discount, matching VS's price. Flanne thought about $4, but VS had set a $3 expectation. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
  - The author argues the low price drove impulse buys instead of wishlist-and-wait behavior. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
  - Results: 110k units in 36 hours, 220k after 4 days, about $538k gross, a 2% refund rate, and a 12,953 concurrent peak. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
- **Brotato** was $5 with a 20% launch discount and had 2M+ Steam sales by June 2023. — [Game Developer](https://www.gamedeveloper.com/press-release/brotato-10-release)
- **Megabonk** was $5.49. — [Notebookcheck](https://www.notebookcheck.net/Vampire-Survivors-esque-indie-Reverse-Bullet-Hell-Game-Megabonk-Sells-1-Million-Copies-in-Just-Two-Weeks.1132354.0.html)
- **HoloCure** is free (a fan game) and reached 45k concurrent players at its Steam launch. — [OpenCritic summary](https://opencritic.com/game/13312/holocure/reviews)

### Inferences
- Genre buyers anchor on about $5. A higher price needs visibly more content or production value. DRG: Survivor had brand support, and Halls of Torment had a Diablo-like look; their prices were not confirmed in this pass.

### Gaps
- DRG: Survivor, Halls of Torment and Soulstone prices were not verified. No controlled data was found on how price elasticity varies across the genre.

## 6. What weaker survivors-likes got wrong

### Takeaway
The press consensus on weak clones covers three things: they are a reskin of the same loop ("the same idea with a Viking theme"), the player gets lost in visual clutter, and nothing distinctive is added (controls, a mobility mechanic, a twist on the build layer). Among games that succeeded, the critiques were repetitive challenges or difficulty spikes (Megabonk) and characters too similar (Soulstone).

### Cited Findings
- **Visual clutter.** Most clones were worse than VS at distinguishing the player from visual clutter (Rock Paper Shotgun). — [Wikipedia: VS-like](https://en.wikipedia.org/wiki/Vampire_Survivors%E2%80%93like)
- **Reskins.** One blogger panned clones where "the idea is basically the same as in any 'Survivor' game, just with a Viking-theme and cartoony graphics." — [search summary, Cubic Creativity clone series](https://cubiccreativity.wordpress.com/2023/07/29/vampire-survivors-clones-2-electric-boogaloo/)
- **Distinctive mechanics that worked.** Praised differentiators in roundups: aiming and reloading (20MTD), a boost mobility mechanic to escape danger, and difficulty modifiers with a speed slider. — [Rogueliker](https://rogueliker.com/bullet-heaven-games-like-vampire-survivors/); [PC Gamer, 2022](https://www.pcgamer.com/so-so-many-vampire-survivors-clones-came-out-this-year/) (via search summary)
- **Discoverability.** Many games sit under broad "Action Roguelite" or "Bullet Hell" tags, which mismatches player expectations. Valve added an official "Bullet Heaven" tag in May 2026, and Steam ran Bullet Heaven fests in 2023-2025. — [Exploring The Games](https://whatsinkedinmymind.substack.com/p/exploring-the-survivors-like-or-bullet-heavens); [Wikipedia: VS-like](https://en.wikipedia.org/wiki/Vampire_Survivors%E2%80%93like)
- **Iteration on feedback.** 20MTD's iteration toward more build variety, enemy variety and difficulty came from community feedback. — [HTMAG](https://howtomarketagame.com/2022/06/14/20-minutes-till-dawn/)
- **Gamalytic, 2025.** About 65.9% of roughly 12,700 Steam releases in 2025 earned under $1k. The figure is not genre-specific. — [search summary, Game World Observer](https://gameworldobserver.com/?p=21233)

### Inferences
- **For Soul Saver**
  - Make the player sprite always readable.
  - Make heroes mechanically distinct, not stat variants.
  - Pick one headline differentiator and surface it in the first 5 minutes of a run. Boss barrier arena plus evolution on kill is the best candidate.
  - Make sure the first run hits a level-up, a chest or evolution-style payoff, and visible screen-filling escalation early.

### Gaps
- No postmortem of a failed survivors-like was found.
- No hard data was found on first-10-minute retention or refund rates outside 20MTD's 2%.
