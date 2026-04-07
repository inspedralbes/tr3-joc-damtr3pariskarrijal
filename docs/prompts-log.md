# Prompts Log

## Feature
Map theme selection and synchronized destructible terrain.

## Purpose of this file
This document records the complete AI-assisted workflow followed for the SDD feature.  
Each iteration must include:

- the prompt used
- the result returned by the AI
- the problem detected
- the correction applied
- the reason for the correction

The goal is to demonstrate traceability, not only the final result.

## Scope of the SDD feature
This prompts log only covers:

- predefined map theme selection before the match
- loading the same terrain preset for both players
- synchronized terrain destruction when a projectile hits the ground

This prompts log does not cover:

- procedural terrain generation
- special bullet types
- ML-Agent behavior
- unrelated UI polishing

---

## Iteration 1

### Objective
Define a small SDD feature that is suitable for the course requirements and realistic to implement.

### Prompt
```text
I need a small SDD feature for my Unity + Node.js multiplayer tank game.
The feature must be concrete, easy to present to a teacher, and connected to shared multiplayer state.
Suggest a bounded feature and explain why it is a good SDD candidate.
```

### AI Result
The AI proposed synchronized destructible terrain as the main feature.

### Problem Detected
The first idea was still too generic and did not yet define whether the terrain should be fixed, generated, or selected by players.

### Correction
The scope was narrowed and clarified through follow-up prompts.

### Reason for Correction
The feature needed clearer boundaries so that another AI could implement it from a spec without guessing.

---

## Iteration 2

### Objective
Decide whether the feature should include procedural hill generation.

### Prompt
```text
Should the SDD feature include procedural generation of different hills for each match, or should it stay fixed?
I want a feature that is strong enough for presentation but still manageable.
```

### AI Result
The AI explained that procedural generation would expand the scope too much and introduce new problems:

- deterministic generation
- seed synchronization
- spawn validation
- terrain playability

### Problem Detected
Procedural generation made the feature too large for a focused SDD slice.

### Correction
Procedural generation was removed from the SDD scope.

### Reason for Correction
The teacher requires a concrete and traceable feature, not an oversized system with too many moving parts.

---

## Iteration 3

### Objective
Add map variety without introducing procedural generation.

### Prompt
```text
Instead of procedural generation, can the feature support predefined map themes like desert, snow, grassland, and canyon?
Players choose one, and both clients load the same preset.
Then terrain destruction happens on top of that preset.
```

### AI Result
The AI proposed a refined feature:

- predefined map theme selection before the match
- one shared terrain preset per map theme
- synchronized destructible terrain during the match

### Problem Detected
The feature now had two connected parts, so the SDD files needed to reflect both:

- map selection
- synchronized terrain destruction

### Correction
The spec was rewritten to include both the pre-match map choice and the in-match destruction behavior.

### Reason for Correction
The implementation AI must know that destruction is applied on top of a selected preset, not on a single hardcoded map.

---

## Iteration 4

### Objective
Reject additional gameplay systems that would make the feature too large.

### Prompt
```text
What if I also add different bullet types, like one infinite basic bullet and some special bullets with one use only?
Should that be part of the same SDD feature?
```

### AI Result
The AI advised against including bullet types in the same SDD feature.

### Problem Detected
Bullet types would add:

- ammo inventory
- usage limits
- shot selection UI
- server validation of ammo state
- synchronization of projectile types

### Correction
Special bullets were explicitly excluded from the SDD scope.

### Reason for Correction
The feature must remain narrow enough to implement, test, and explain clearly.

---

## Iteration 5

### Objective
Create formal SDD specification files for another AI to implement.

### Prompt
```text
Create the SDD files for this feature:
- predefined map theme selection before the match
- one shared terrain preset per map theme
- synchronized destructible terrain when a projectile hits the ground

Do not include procedural generation.
Do not include special bullet types.
Write:
- specs/foundations.md
- specs/spec.md
- specs/plan.md
```

### AI Result
The AI produced:

- `specs/foundations.md`
- `specs/spec.md`
- `specs/plan.md`

### Problem Detected
None at this stage. The files matched the intended scope.

### Correction
No correction required in this iteration.

### Reason for Correction
Not applicable.

---

## Current Final Scope

The final SDD feature is:

**Players choose a predefined map theme before the match starts, both clients load the same terrain preset for that theme, and projectile impacts cause synchronized terrain destruction during the match.**

### Explicitly Included

- predefined map themes
- match-level `mapType`
- identical preset loading on both clients
- projectile-to-terrain collision
- synchronized terrain destruction event
- collider refresh
- simple tank grounding correction

### Explicitly Excluded

- procedural map generation
- random terrain per match
- special bullet inventory
- ML-Agent behavior
- advanced terrain physics

---

## Template for Future Iterations

Copy this block for each new AI iteration:

```md
## Iteration X

### Objective
Describe what you wanted to achieve.

### Prompt
```text
Write the exact prompt here.
```

### AI Result
Summarize what the AI produced.

### Problem Detected
Explain what was wrong, missing, too broad, too narrow, or inconsistent.

### Correction
Explain what you changed next:
- prompt
- spec
- plan
- implementation approach

### Reason for Correction
Explain why that change was necessary.
```

---

## Notes for the Final PDF

When converting this work into the final reflection PDF, explain:

- why this feature was chosen
- why procedural generation was rejected
- why special bullet types were rejected from the same SDD scope
- how the final feature stayed aligned with the specification
- whether the implementation AI followed the spec correctly
- where the AI needed correction or clarification
