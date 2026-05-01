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

## 4. Reflection on This Project (one paragraph)

[Hardest part of building YOUR W15 project specifically? What would you
build NEXT on top of it if you had another week? Save the broader
semester reflection for Section 5.]

> **Example:** *"The hardest part was figuring out why my Shop migration
> kept breaking — I'd forgotten to add the discriminator value in
> GameContext, so EF Core kept generating an empty migration. Took me an
> hour with the migration script generator to spot it. If I had another
> week I'd add a quest log, because I think the data model would be a fun
> exercise (Quest entity + Player→Quest many-to-many + a goal-state checker)."*

---

## 5. Course Feedback (NOT graded — please be candid)

Help me make this class better next time. **This section is not part of
your project grade.** I read it after grades are submitted, and I'd much
rather hear "week X was painful because Y" than diplomatic non-answers.
Concrete > polite.

**What did you learn that genuinely stuck with you?**
[One specific concept or skill — be concrete. "TPH" is fine; "`OfType<T>()`
finally clicked when I had to filter loot drops" is better.]

**What did you like about the course?**
[Pacing, projects, format, in-class examples, READMEs, anything.]

**What didn't work for you?**
[What was confusing, slow, repetitive, or disconnected from the rest of
the work? Be specific so I can actually fix it.]

**What surprised you?**
[Something you expected to be easy but wasn't, or vice versa. Or a moment
where a concept clicked unexpectedly.]

**What was the hardest part of the semester (not just this project)?**
[A particular week, concept, debugging session, or assignment. Why?]

**What would you ADD to next year's version?**
[A topic, a tool, more practice on something, a guest speaker, anything.]

**What would you REMOVE or shorten?**
[Anything that felt like filler, redundant, or off-topic from your
perspective. Honest answers help — I literally rewrite the rubric and
materials each year based on this section.]

**Anything else?**
[Open-ended. Wins, frustrations, advice you'd give future students,
requests, or just a sentence about your overall experience.]

---

## How this is graded

**Sections 1-4** are the **gate to all rubric tiers.** Without a complete
and honest accounting of your starting point, additions, sources, and
project reflection, the project caps at 50% regardless of code quality.

- **Base/B/A/A+** all require Sections 1-4 to be filled out and to match
  what's actually in your repo.
- During your final presentation I may ask you to walk through any file
  you describe yourself as "added" or "modified" — be ready.
- Using template code with clear attribution is fine and earns full credit.
  Claiming to have written code you didn't is not, and will be graded
  as such (zero on the affected tier).

**Section 5 is not graded.** It exists to make the class better. A blank
Section 5 won't lower your grade; an honest critical Section 5 won't
either. The only "wrong" answer there is a fake one.

Think of Sections 1-4 as the README every PR needs: a short story about
what changed and why. It's a real engineering skill, and it's the most
reliable way for me to grade what you actually did.
