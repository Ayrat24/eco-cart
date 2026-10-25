# Eco Cart — game loop and growth review

Review date: 25 October 2026. Scope: current gameplay source, upgrade/world assets, UI logic, project documentation, and three reference store pages. Unity CLI detected the running project but could not reach its Pipeline server. This is a design and configuration review, not a hands-on assessment of animation, sound, controls, or actual session pacing. No gameplay assets or code were changed.

## Recommendation

Build toward **a cozy cleanup incremental where a tiny cart becomes an animal-powered restoration operation**. The cart, physical cargo, returning greenery, and animals are the strongest available ingredients for a recognizable identity. The progression should change what players can accomplish per minute and what the restored place looks like.

The current risk is a pleasant collection activity attached to a money-growth system: more money does not consistently translate into dramatically better cleaning. Larger maps amplify this mismatch.

Working commercial assumption: a finite, premium PC game with an optional postgame. This is a proposed direction, not an established platform or business requirement. A mobile idle product would require a separate input, session, offline-progress, and monetization design.

## Current loop and evidence

Move using right mouse input → nearby hands collect physical scrap → cargo fills → left click recycles the load → money purchases value upgrades, carts, trees, tools, and helpers → clean another area or select another world.

Recycling is available through the player/cart; a mandatory trip to a fixed depot is not part of this code path. Preserve that convenience unless logistics becomes an intentional, tested mechanic.

Trees add a parallel loop: buy a tree → nearby eligible ground becomes green → purchased tree levels produce periodic money → buy more production. Digging piles provides another active activity. Currency and upgrades carry between worlds.

| Finding | Source | Design implication |
|---|---|---|
| Five worlds use side sizes 3, 5, 8, 10, 14; scrap per chunk rises from 2 to 5. | `ContentDescriptions/Worlds/WorldPreset 1..5.asset` | Interior area grows from 25 to 729 chunks: about 29×. An all-field scrap-count proxy rises from 50 to 3,645: about 73×. Actual totals differ because later worlds include piles. |
| Collection attempts happen every 10 frames, at most one item per scan; hands can further limit collection. | `Scripts/ItemCollecting/ItemCollector.cs` | A fixed collection pipeline constrains the payoff from larger worlds and faster vehicles; frame-based timing also needs normalization before reliable balancing. |
| Recycling waits 0.15 seconds per item, plus a final 0.6 seconds; collection pauses while emptying. | `Scripts/ItemCollecting/ItemRecycler.cs`, `Cart.cs` | An 80-item load spends at least 12.6 seconds recycling, excluding hand completion and cart setup delay. Capacity is weight, so this example only applies to 80 one-weight items. |
| Cart capacity/speed pairs are 10/8, 20/8, 40/8, 80/7, and 40/25. | `ContentDescriptions/Carts/` | The truck is a speed specialization, not a straightforward capacity upgrade; communicate this or redesign the progression. |
| Scrap value rises approximately linearly with level, while listed scrap upgrade costs multiply by 1.75. | `Scripts/Upgrades/ScrapScoreUpgrade.cs`, scrap upgrade assets | Repeated value upgrades get progressively less attractive unless another system compensates. |
| Same-type items build a multiplier within a recycled load. | `Scripts/ItemCollecting/ItemRecycler.cs` | Batch composition can be a useful decision, but players need a payout preview and a way to influence collection. |
| Only one helper is active; selecting another replaces it. | `Scripts/Helpers/HelperManager.cs` | Companions offer variety but cannot yet become a growing workforce. |
| Tree earnings use purchased levels; planting may return without placing a tree when no eligible tile is found. | `Scripts/Trees/TreeTypeEarner.cs`, `World/TreePlanter.cs` | Income and visible restoration can diverge. Successful placement should authorize the purchase, or the tree should remain visibly pending. |
| Apple tree score upgrade has cost growth 1, while its payout includes exponential growth. | `ContentDescriptions/Trees/AppleTree/AppleTreeScore.asset`, `TreeScoreUpgrade.cs` | A constant-price compounding investment may become the dominant strategy. This is a balance risk, not a measured exploit. |
| Clean and green percentages use `1 - (1 - x)^2`; the next-world prompt checks only clean percentage. | `Scripts/World/ProgressTracker.cs` | If the denominator is valid, displayed 90% corresponds to about 68.4% raw completion. The meter and completion rule communicate different things from literal cleanup. |
| World selection builds selectable entries without a completion gate. | `Scripts/WorldSelector.cs` | The next-world popup is a suggestion, not a demonstrated unlock. Choose whether the game is a campaign or freely selectable contracts. |
| Tutorial UI starts closed; the map is a purchased unlock. | `Scripts/UI/TutorialMenu.cs`, `ContentDescriptions/Unlocks/MapUnlock.asset` | New players may lack both a directed first goal and navigation. |

Paths in this table are relative to `Assets/Eco/`. Comments and architecture documentation are occasionally out of sync with implementation; the observations above use implementation where they disagree.

## Prioritized changes

Effort is relative: small means localized tuning/UI, medium means a system spanning several components, large means a new production capability. These are not schedule estimates.

| Priority | Change | Player benefit and implementation direction | Effort |
|---|---|---|---|
| P0 | A compact opening restoration job | Start at a recognizable trashed picnic spot. Collect, recycle, purchase one visible improvement, and restore the spot before opening the larger island. | Medium |
| P0 | Upgrades that change cleaning throughput | Add pickup radius, hand speed/count, faster batch recycling, and eventually auto-recycling. Make each major tier visibly alter the cart. | Medium |
| P0 | Bound recycling interruption | Increase animation speed with load size, group repeated items, or let an upgraded recycler process while collecting. Preserve a satisfying payout crescendo. | Small–medium |
| P0 | Balance active and passive income together | Model purchase payback and cleaning time. Remove constant-price compounding outliers and audit the shared cost formula, which currently uses growth^(level+1) for positive levels. | Medium |
| P0 | Trustworthy completion | Use literal completion counts. Offer contract completion around 80–90% if desired, with optional 100% mastery. Track actual generated cleanup work, including piles, rather than assuming every interior chunk contains the same scrap count. | Medium |
| P0 | Coherent world progression | Choose explicit campaign unlocks or clearly labeled free-choice contracts. State what persists and what each completed world awards. | Small–medium |
| P1 | Two or three complementary helpers | Begin with collector + planter. Give them assigned work areas and visible results. Keep an active player role in finding routes, exposing piles, and handling special objects. | Large |
| P1 | District-sized goals | Divide large maps into recognizable pockets with local progress and completion payouts. Players should finish a place before committing to the next. | Medium |
| P1 | Restoration milestones | Restore a pond, reopen a playground, repair a bridge, or bring wildlife back. Make these outcomes visible from the normal gameplay camera. | Medium |
| P1 | Mechanical identity for each biome | Beach: sweeping light debris; scrapyard: heavy items and magnets; wetland: unblock water to restore terrain. Add one new interaction and one new tool per biome. | Large |
| P1 | Cart specializations with clear tradeoffs | Cargo cart for bulk, collector cart for dense patches, fast cart for exploration. Allow inexpensive switching and show capacity, pickup rate, and recycle time together. | Medium |
| P1 | Readable batch bonuses | Show expected payout, type counts, and bonus before recycling. Later add a collector filter or sorting attachment; avoid requiring manual sorting of every item. | Medium |
| P1 | Permanent restoration rewards | Completed districts earn research unlocks or equipment sockets. Keep restored islands visitable. Do not require turning a beautiful island back into garbage to progress. | Medium |
| P1 | Basic map and cleanup assistance | Give basic navigation immediately; unlock a scanner for remaining debris later. Near completion, let helpers finish stragglers or highlight them clearly. | Small–medium |
| P1 | Controls and persistence for short sessions | Add direct movement/controller support, remapping, hold/toggle options, readable UI, and periodic/checkpoint saves. Verify cargo and in-flight recycling are handled consistently. | Medium |
| P2 | Discoveries under the trash | Find keepsakes, small stories, decorations, and helper encounters. These give players a reason to clear a particular place beyond its cash value. | Medium |
| P2 | Before/after presentation | Record district viewpoints; show transformation on completion and offer a photo mode. This also creates useful store and social footage. | Small–medium |
| P2 | Optional repeatable contracts | After a satisfying campaign ending, generate new jobs with selected modifiers. Keep completed campaign spaces intact. | Large |

Do not implement every row at once. The first validation milestone should contain the P0 work, one distinctive helper, and one memorable restoration reveal.

## Proposed progression

**Moment to moment:** steer through a satisfying patch → watch debris collect → cash out → immediately see a useful purchase approaching.

**Several minutes:** finish a district → restore its landmark → gain a helper or cart capability → use it on a visibly bigger challenge.

**Across sessions:** complete islands → earn permanent equipment options → discover a new environment with a different cleaning problem → ultimately finish a regional restoration project.

Early, middle, and late gameplay should visibly differ: a few hand pickups; a cart sweeping a whole patch; a coordinated crew restoring a district while the player handles its main obstacle. Increasing currency digits alone does not demonstrate this progression.

Suggested first-session targets, to validate through playtests rather than treat as established timings:

- First 30 seconds: collect and recycle without reading a help page.
- By 2 minutes: buy an upgrade whose effect is immediately observable.
- By 5 minutes: finish a small area and see a clear environmental transformation.
- By 10 minutes: acquire a helper that removes one repetitive task.
- By 15–20 minutes: finish the opening contract and glimpse the next capability.

The demo should complete a small emotional arc. Leaving the player halfway through a giant percentage bar is a weaker stopping point than showing a restored place and a compelling next job.

## Economy and content scaling

Track two outcomes separately: **money earned per minute** and **world cleaned per minute**. A reward multiplier improves the first without necessarily improving the second. World workload needs to be balanced against the second.

For each key upgrade, record price, current earning rate, marginal earning rate, actual collection rate, recycle downtime, and estimated time to the next meaningful unlock. Passive investment payback is price divided by additional income per second. Also compare the purchase against a throughput upgrade, because finishing a contract faster can be more valuable than raw income.

For an active-first game, test a rule such as: playing well should produce at least twice the near-term progress of waiting at the same stage. This is an initial hypothesis, not a universal genre benchmark. Passive income should help players resume and invest; it should not make cleaning feel like a financially irrational chore.

Use reusable district templates: landmark + debris arrangement + cleanup mechanic + restoration state + reward. Handcraft the opening and major reveals; vary secondary debris placement procedurally. This creates authored pacing without requiring every piece of litter to be placed by hand.

For technical scale, separate logical cleanup work from rendered clutter. Simulate distant helpers using district work budgets; materialize their actions when nearby. Use a bounded amount of physical cargo to represent a larger logical inventory. Preserve visible cause and effect and prevent simulated workers from taking rewards the player is actively interacting with. The existing chunk pooling and stress-test infrastructure are a useful foundation, but no new performance validation was performed in this review.

## Marketability

Proposed pitch: **Turn a trash-covered island into a thriving sanctuary with an upgradeable cart and a crew of animal helpers.**

This is a positioning hypothesis. The important visual promises are a memorable cart silhouette, an unmistakable dirty-to-restored transition, and companions performing understandable jobs. Show these in the opening seconds of promotional footage. Show the early and late cart doing the same task so the acceleration is obvious.

Reference products, checked on their Steam pages:

- [Loddlenaut](https://store.steampowered.com/app/1644940/Loddlenaut/) combines cleanup with care for creatures. Design lesson: make the beneficiaries of restoration emotionally recognizable.
- [Outpath](https://store.steampowered.com/app/2237970/Outpath/) presents incremental gathering, building, and island expansion. Design lesson: escalating capability needs new goals and spaces to act on.
- [A Game About Digging A Hole](https://store.steampowered.com/app/3244220/A_Game_About_Digging_A_Hole/) presents a simple collect/sell/upgrade premise with a discovery hook. Design lesson: a concise promise and a reason to investigate can support a focused scope.

These comparisons establish reference mechanics and positioning, not sales forecasts or proof of demand for Eco Cart. Validate the cart-and-crew hook with actual footage and unfamiliar players before setting price, playtime promises, or a large content budget.

For a first release, defer online co-op, an elaborate factory simulation, recurring-event obligations, and a mandatory prestige reset. Each adds substantial design or production scope before the core transformation has been proven. Optional bounded offline earnings can be considered later, after deciding how much the game should reward absence.

## Validation plan

Run an initial directional test with 5–8 unfamiliar players. Observe without explaining the controls or the economy. This is qualitative testing, not a statistically reliable retention study.

Record first pickup, first recycling action, first purchase, first district completion, first helper, and voluntary stopping point. Measure time spent cleaning, travelling, recycling, and inside menus. Record active versus passive income and whether a purchased upgrade improves district completion time.

Ask players to explain their next goal, which upgrade changed their play, why they would continue, and what makes the game distinctive. A strong opening lets them answer those questions without assistance.

Recommended next prototype: one compact island, three visually different cart stages, one useful animal helper, three district restorations, and an ending. Expand content only after that slice demonstrates satisfying acceleration and a clear reason to play another island.
