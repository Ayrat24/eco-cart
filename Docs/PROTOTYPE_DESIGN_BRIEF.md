# Eco Cart prototype design brief

Date: 25 October 2026

Build a prototype around the physical pleasure of picking rubbish off the ground, placing it into a cart, and watching it return to physics and settle among the other objects. Progression should make that activity more rewarding and connect it to permanently restoring a place.

This brief records the direction developed in our design discussion. **Decided** means an explicit preference or accepted direction. **Prototype proposal** means a recommendation to test, not a settled requirement. Numeric examples are initial tuning hypotheses.

## What the prototype needs to establish

The developer's playtests found that some players, mostly young women, enjoyed collecting and recycling. Others asked what the point was. Few engaged with upgrades, and the first level could be completed comfortably without them.

That supports further testing of the collection interaction. It does not yet establish long-term engagement or commercial demand. The next prototype should test whether a visible goal and a desirable physical upgrade motivate the players who already enjoy collecting.

Core experience:

**Choose a patch to clean → physically collect a load → optionally adjust its contents → recycle → buy a tangible improvement → restore more of the same world.**

Non-negotiable principles:

- Preserve the hand-to-object-to-cart interaction and physical release.
- Keep automatic pickup and relaxed movement. Do not add a material filter.
- Cleared areas stay cleared. Do not respawn rubbish to feed the economy.
- Ordinary, unsorted collection must provide enough rewards to complete required progression.
- Spending poorly must not permanently block progress.
- Upgrades should improve an experience that is already pleasant, without making the starting version deliberately irritating.

## Upgrade menus and progression

### Direction

**Prototype proposal:** separate three player intentions: improving capabilities, placing restoration objects, and equipping owned items. Keep related screens in one consistent menu shell; planting should leave the world visible.

| Interface | Contents | Interaction |
|---|---|---|
| Upgrades | Collection, recycling, handling, digging, future automation | Compact progression tree with repeatable upgrades inside relevant nodes or cards |
| Plant and restore | Trees, flowers, habitats, restoration objects | Small toolbar, world placement preview, confirm or cancel |
| Equipment and crew | Owned carts, attachments, outfits, hats, active helpers | Select equipment and preview its effect |

Use the term **Equipment**, rather than adding a conventional backpack inventory. Material counters can be automatic; the physical cart already provides the collection capacity constraint.

### What belongs in the tree

Use nodes for meaningful capability changes, such as two-handed handling, access to a new object class, or improved recycling. Do not make every repeated value increase a separate node. A large tree is not the immediate prototype objective.

The opening needs a purchase players want before the area is finished. Put a tangible cart or handling improvement before relying on repeated scrap-value upgrades to create motivation. Show the next cart and its actual benefit before purchase, make it affordable after a few loads, and leave enough rubbish to enjoy it afterward.

Success is not merely that players press the purchase button. They should notice the difference and begin anticipating another improvement.

### Planting

If included, let players select a tree and preview its location and greening radius in the world. Make repeated placement convenient. Charge only when placement succeeds, or retain a clearly visible pending placement; avoid paying for an invisible tree that produces income without being planted.

Automatic planting is a possible later upgrade, not needed for the first slice.

### Economy scope

Material-specific resources alongside universal credits were discussed as an option, not adopted as a requirement. Keep the first slice's economy small. It can use money while testing the same-type bonus. Introduce separate materials only if they create useful decisions and have clearly understood uses.

If essential purchases use finite materials, guarantee sufficient supply and a fallback exchange. Avoid recipes requiring many resource types merely to add complexity.

### Acceptance checks

- Players can identify what the next important purchase does before buying it.
- The first major upgrade changes collection or carrying in a visible way.
- The opening remains enjoyable before that purchase.
- Menu use does not dominate the collection session.
- The player can continue after spending money on optional choices.

## Humanoid character and full body IK

### Direction

**Decided:** the current generic ghost enemy was a jam-time solution that made extending arms with simple IK convenient. Explore a humanoid replacement while keeping the character personally picking up objects and placing them in the cart. Do not move this core action onto cart-mounted mechanical arms.

**Prototype proposal:** use full-body IK with authored motion templates. Adapt reaching, body bend, crouching, and hand targets to the actual object and cart position. Full-body motion should reinforce the interaction rather than repeatedly interrupt movement.

A female protagonist is the intended art direction under consideration, informed by the developer's audience goal. Exact visual style remains open. Character replacement is not, by itself, a validated marketability improvement.

### Pickup sequence

1. Automatically select an eligible nearby object and reserve it.
2. Approach or align only as much as needed for a convincing reach.
3. Blend an appropriate pickup motion with IK for hands, torso, and feet.
4. Attach or control the object during the lift so it does not fight physics.
5. Carry it to an appropriate release position over the cart.
6. Release it into physical simulation with controlled velocity.
7. Let it collide and settle into the load, with readable impact feedback.

Initial handling variants:

| Object | Motion to prototype |
|---|---|
| Small object | Quick one-handed reach and placement |
| Low object | Deeper bend or crouch with stable feet |
| Bulky or heavy object | Two-handed grip, readable effort, heavier release |

Awkward-object variants can follow if the first three feel good. Do not build a full animation catalogue before validating the basic rig.

### Progression constraints

Faster pickup upgrades can shorten reach and recovery while preserving a readable release. Strength can unlock larger objects and different handling. Capacity should create more impressive visible loads.

Avoid accelerating later gameplay until objects effectively teleport into storage. The physics payoff must survive upgrades. Keep collection visible from the normal gameplay camera, including when the cart is full.

### Acceptance checks

- Compare the humanoid against the ghost using the same cart and objects.
- Small, low, and bulky pickups look distinct and remain responsive.
- Hands contact objects plausibly; feet do not slide excessively during crouches.
- Objects release cleanly without clipping through the cart, explosive collisions, or duplication.
- Players can see and enjoy objects landing in the pile.
- Character movement adds satisfaction without making routine collection sluggish.

## One persistent world

### Direction

**Decided:** remove world switching as the main progression structure. Use one world that can expand in scope, with permanent cleanup. Finite rubbish and a satisfying ending are compatible with incremental progression.

**Prototype proposal:** organize the world into connected restoration districts with recognizable landmarks and local completion goals. Build one small district first, plus a visible adjacent area to establish continuity.

### Increasing challenge

Use three layers of work within a district:

- **Easy litter:** dependable progress and opportunities to feel stronger than before.
- **Current challenge:** introduces a new handling or access problem using available capabilities.
- **Optional future challenge:** a conspicuous oversized object that gives players a reason to return.

An early park might have cans, bottles, bags, and an abandoned refrigerator. The refrigerator must not prevent completing the park's essential objective with starting capabilities.

Later districts can increase density, object bulk, route complexity, or introduce buried scrap. Do not make every later object slower to handle. Maintain light rubbish so improved equipment has obvious benefits.

Believable settings can suggest different collection patterns: a park with scattered litter, a market with dense packaging, a canal with narrow approaches, and a workshop with bulky objects. These are content examples, not a commitment to build all four.

### Preventing progression dead ends

**Recommended structure:** grant essential capabilities directly through restoration milestones; use spendable money for efficiency, equipment choices, and cosmetics.

Example: completing the picnic ground awards a lifting capability, which enables bulky bags and prepares the player for the next district. The capability cannot be accidentally spent on an outfit.

Safeguards:

- Each required district objective is achievable with capabilities available on arrival.
- The reward for that objective prepares the player for subsequent required work.
- Optional oversized objects do not block essential paths or objectives.
- Required rewards are achievable with no sorting bonus and without optimal purchases.
- Functional upgrade refunds or exchanges are a possible additional safety net.
- Never place all of a required material behind the capability purchased with that material.
- Communicate unavailable handling requirements and where the capability is earned.

Dragging suitable heavy objects was discussed as an optional fallback. It is not necessary for the first slice if placement and progression already prevent obstruction.

### Completion

Show literal progress or explicit objective counts. Avoid silently presenting an eased percentage as the actual percentage cleaned. Main restoration completion can occur before optional 100 percent cleanup, but label those goals separately.

Finishing a place should produce a visible payoff, such as making a picnic spot usable or revealing restored ground. Preserve it when the player moves onward.

### Acceptance checks

- Deliberately spend all available money on optional purchases; essential progression remains possible.
- A heavy object cannot trap the player or prevent a required goal prematurely.
- Cleared areas remain clean after leaving, returning, and reloading.
- Players understand their local goal and can see what completion changed.

## Combo system and throwing out scrap

### Direction

**Decided direction:** encourage matching loads through route planning, allow ejection to correct unwanted pickups, use a forgiving bonus curve, and adjust generation to suggest subtle paths of similar material. Do not add a collection filter.

The bonus should reward an intention without requiring precision selection of every object. Unsorted collection remains a complete way to play.

### Material distribution and readability

Generate loose, overlapping material clusters that suggest routes. Anchor them to plausible sources: paper near benches, packaging around delivery areas, and bottles near picnic spots. Avoid rigid uninterrupted trails that feel mandatory.

Use recognizable object silhouettes and consistent categories. A brief material icon on pickup and a matching cargo icon can teach classification. Avoid relying on color alone. If an object is ambiguous, show its category consistently.

### Ejecting an object

**Prototype interaction:** selecting a cargo icon highlights the corresponding physical object. A quick action makes the character remove it and place or toss it a short distance beside the cart, returning it to physics.

Requirements:

- Selecting one object must remain practical when physical objects overlap.
- Ejected objects must not be immediately collected again. Use an intentional ignore state, with a clear rule for restoring eligibility; exact input and reset rule remain to be tested.
- Keep discarded items reachable and inside the playable area.
- Preserve their identity and save state; ejection cannot grant recycling rewards or duplicate objects.
- Cleanup accounting must follow the moved rubbish. Moving it into a cart or another district cannot falsely complete cleanup.

Setting something aside for another load fits the restoration theme better than launching it far away. Ejection is intended as an occasional correction, not the main activity.

### Bonus curve

Calculate a bonus separately for the amount of each material in the load. Let the multiplier rise and then flatten. The extra improvement should diminish; the payout already earned should never be reduced by adding another item.

Conceptual calculation:

`Load reward = sum of each material's base reward multiplied by its amount-based bonus`

Calculate from the complete load, independently of the order in which objects animate out of the cart. Choose a consistent amount measure; cargo units or weight are preferable to raw object count if sizes vary substantially.

Adding glass to eight paper units must not reduce the paper subtotal. Glass occupies space that could hold more paper, which is enough opportunity cost to make composition meaningful.

Illustrative balance targets for a cart holding ten equal-value, equal-size units:

| Composition | Base reward | Example reward with bonuses |
|---|---:|---:|
| 10 paper | 10 | 20 |
| 8 paper and 2 glass | 10 | 17.4 |
| 5 paper and 5 glass | 10 | 17 |
| 2 each of five materials | 10 | 11 |

These examples describe desired behavior, not a finalized formula. A mostly matching cart should retain most of a pure load's value. Start by testing an 80 percent matching load at roughly 85 to 95 percent of a pure load's reward, assuming comparable base values.

Ensure the curve does not unintentionally make splitting into small mixed groups or repeatedly recycling tiny loads the dominant strategy. Test composition, capacity upgrades, and recycling trip time together. Whether bonus thresholds scale with cart capacity remains a tuning decision.

### Feedback and completion

Show expected recycling output and the current material bonus while collecting. Use a clear label such as Paper bonus or Sorting bonus; do not imply a timed streak that expires.

As the world empties, perfect loads become harder to form. District completion rewards and optional mastery should make finishing leftovers worthwhile. Never require a pure load or a full cart to recycle or complete required work.

### Acceptance checks

- Players can recognize at least one useful material route without an explanation.
- Mixed loads still feel worthwhile, and exact bonus amounts are visible before unloading.
- Ejection does not create a pickup-and-throw loop or unreachable leftovers.
- Normal play involves occasional corrections rather than constant cargo management.
- Completing the slice with no bonus remains economically possible.
- Reloading preserves ejected items and accurate cleanup state.

## Recycler and collection trips

### Status

**Prototype proposal, not a final decision:** test a fixed recycler to add a collection-trip rhythm and a physical unloading payoff. Compare it against the current recycle-anywhere behavior. The main risk is replacing enjoyable collection with repeated travel through already cleared space.

### Proposed interaction

Enter a generous drop-off zone and unload with little input friction. Tip the cart and let its contents fall into a hopper. Clearly show the material totals and sorting bonus. Let the player leave promptly; prolonged processing can happen independently.

The complete physical sequence becomes **ground to hands to cart to recycler**. The station should make that sequence more satisfying, not merely impose a commute.

### World progression

If fixed recycling proves worthwhile, expand the network as the world opens:

| Stage | Arrangement |
|---|---|
| Opening | A nearby recycler serves the first compact district |
| Expansion | Restoration milestones activate local recycling points |
| Later possibilities | Collection service, remote unloading, or a mobile recycler |

Guarantee essential access to new recycling points. Money can improve processing or convenience, but optional spending must not leave the player with an unreasonable commute.

For the first layout, test roughly 10 to 20 percent of a collection cycle spent returning and unloading. This is an initial pacing hypothesis, not an accepted target. Measure it again after nearby rubbish is gone, when the journey is empty.

### Acceptance checks

- Returning adds anticipation and a satisfying unload rather than frequent frustration.
- The drop-off zone is easy to enter; players do not need precise cart alignment.
- Physical unloading remains readable and does not require per-item clicks.
- The loop stays pleasant after the nearest area has been cleared.
- Compare voluntary continuation and travel complaints with recycle-anywhere play.
- Remove or revise mandatory return trips if their benefit does not justify lost collection time.

## Recommended prototype scope

Build a small slice rather than the complete future world. Use placeholder art where needed; a humanoid rig test does not require the final wardrobe or finished protagonist.

Suggested implementation order:

1. Establish a compact district, an understandable restoration goal, and persistent cleanup.
2. Surface one desirable physical cart upgrade early, leaving enough work to enjoy it.
3. Test humanoid pickup and release with small, low, and bulky objects.
4. Add live load reward previews, the tapered material bonus, and safe item ejection.
5. Shape material clusters into optional routes within the same district.
6. Test one nearby recycler against the existing recycle-anywhere loop.
7. Validate a guaranteed capability milestone and an optional heavy object before expanding the world.

Keep changes separable so a disappointing result can be traced to a specific system. A prototype toggle between recycler modes and between bonus-enabled and base-reward play would help.

Defer a large ability tree, a multi-currency crafting economy, many biomes, a full character creator, helper teams, and advanced automation until the slice demonstrates their need.

## Playtest checklist

Recruit fresh players who enjoy relaxed collection or restoration games, including women similar to those who responded well to the current build. Observe behavior before explaining systems or asking whether players like them.

Record:

- Time to first collection, recycling, upgrade purchase, and local completion.
- Whether players understand the next goal and the upgrade's benefit.
- Time spent collecting, travelling, unloading, and managing cargo.
- Ejection frequency and whether it feels like correction or repeated maintenance.
- Whether matching loads emerge naturally from route choice.
- Whether the humanoid motion improves satisfaction without slowing control.
- Whether players voluntarily want to continue after completing the area.

Run a deliberate baseline playthrough with mixed loads, early unloading, and poor optional spending. It must still reach the required ending with the world's finite resources.

The prototype succeeds when players enjoy the physical interaction, want a tangible improvement, understand why they are restoring the place, and choose to continue. A larger feature list is not the success criterion.
