# CONTRIBUTIONS

> **Required for ALL tiers.** Replace the bracketed prompts with your own
> answers. Honesty is graded; volume is not. One paragraph per section is
> usually enough — be specific, not impressive.

---

## 1. Starting Point

[Tell me where you started: your own repo carried forward from W14, the W15
template, or a hybrid?]

> **Example:** *"I started from my own W14 repo and pulled MapManager.cs and
> ExplorationUI.cs from the W15 template. My existing GameContext, models,
> and migrations carried forward unchanged."*
>
> **Or:** *"I started from a fresh clone of the W15 template because my own
> code stopped working after W13 and I couldn't recover it."*

---

## 2. What I Added

[List the things YOU added on top of your starting point. For each, one
sentence on what it does.]

> **Example:**
> - `AdminService.MostDangerousRoom()` — LINQ `GroupBy` on `Monster.CurrentRoomId`
>   summing Health to find the riskiest area. Wired into the admin menu.
> - New `Orc : Monster` subclass with a `Strength` stat. New migration
>   `AddOrcMonster`, plus one seed row added to `SeedFinalWorld.sql`.
> - Added a `Shop` container in Town Square that exchanges items for gold.
>   New `Shop : Container` subclass, `Player.Gold` field, two new migrations.

---

## 3. What I Used From the Template / AI / Other Sources

[Honest list. Using template code or AI assistance with attribution is fine.
Pretending you wrote something you didn't is not.]

> **Example:**
> - `MapManager.cs`, `ExplorationUI.cs`: used as-is from the W15 template.
> - `GameEngine.HandleChest`: copied from the W15 template, then modified
>   to add a "magical inspect" option for cursed chests.
> - `AdminService.MonsterCensus`: my own code, but the `GroupBy` pattern is
>   based on the W12 in-class `ListItems` example.
> - GitHub Copilot helped me draft the regex in `Shop.ParseTradeCommand`.
>   I rewrote ~half of it after testing.

---

## 4. Reflection (one paragraph)

[Hardest part of the project? What would you build if you had another week?
What did you learn that surprised you?]

> **Example:** *"The hardest part was figuring out why my Shop migration
> kept breaking — I'd forgotten to add the discriminator value in
> GameContext, so EF Core kept generating an empty migration. Took me an
> hour with the migration script generator to spot it. If I had another
> week I'd add a quest log, because I think the data model would be a fun
> exercise (Quest entity + Player→Quest many-to-many + a goal-state checker)."*

---

## How this is graded

This file is the **gate to all rubric tiers.** Without it, the project
caps at 50% regardless of code quality. With it:

- **Base/B/A/A+** all require a complete, honest CONTRIBUTIONS.md that
  matches what's actually in your repo.
- During your final presentation I may ask you to walk through any file
  you describe yourself as "added" or "modified" — be ready.
- Using template code with clear attribution is fine and earns full credit.
  Claiming to have written code you didn't is not, and will be graded
  as such (zero on the affected tier).

Think of this as the README every PR needs: a short story about what
changed and why. It's a real engineering skill, and it's the most reliable
way for me to grade what you actually did.
