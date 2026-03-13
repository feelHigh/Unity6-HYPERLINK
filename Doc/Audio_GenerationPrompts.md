# Audio Generation Prompts - HYPERLINK

Complete guide for generating all audio assets using **Suno v5** (BGM) and **ElevenLabs** (SFX).

---

## Quick Reference

| Tool | Audio Type | Count |
|------|-----------|-------|
| **Suno v5** | BGM Music (2:00+ duration) | 7 tracks |
| **ElevenLabs** | SFX (one-shots) | 43 sounds |
| **Total Assets** | All audio | **50 files** |

### Existing Audio Assets (8 files)

These files already exist in `Assets/_ProjectHYPERLINK/AudioClip/`:

| File | Maps To | Status |
|------|---------|--------|
| `Boss Attack.wav` | BossAttack | Exists |
| `BossDeath.wav` | BossDeath | Exists |
| `BossSpecialAttack.wav` | BossSpecialAttack | Exists |
| `EpicMonsterAttack.wav` | EpicSpecialAttack | Exists |
| `PlayerDeath.wav` | PlayerDeath | Exists |
| `PlayerTakeDamage.wav` | PlayerHit | Exists |
| `Eclipse Override (1).mp3` | BGM (TBD) | Exists |
| `Final Breath of Stars (1).mp3` | BGM (TBD) | Exists |

### Audio Style Direction

**Inspired by:** Diablo 3/4 dark atmosphere, modern Korean urban horror, dark electronic production

| Aspect | Direction |
|--------|-----------|
| **Overall Tone** | Dark, ominous electronic with sinister undertones — Diablo-like foreboding atmosphere |
| **Core Sound** | Heavy synth bass, distorted drones, oppressive percussion, minor keys |
| **Setting** | Urban buildings: entertainment agency, nightclub, cursed underground floors |
| **Combat Feel** | Fast-paced hack & slash, aggressive and threatening electronic energy |
| **Lore** | Korean folklore demon (Dueoksini) hunting — gothic horror meets modern urban |
| **UI Feel** | Clean but cold, high-tech with dark undertones |

---

# Part 1: Suno v5 BGM Prompts

## 1.1 Suno v5 Simple Mode Interface Guide

### Interface Layout

| Field | Description |
|-------|-------------|
| **Song Description** | Main text area for your prompt (500 char limit) |
| **+ Audio** | Upload reference audio (optional) |
| **+ Lyrics** | Add custom lyrics (optional) |
| **Instrumental** | Toggle ON for no vocals |
| **Inspiration** | Quick style tag chips |

### How to Use Simple Mode

1. Go to https://suno.com/ and click **Create**
2. Select **Simple** mode (left tab)
3. Ensure model is **v5**
4. Paste the full prompt into **Song Description**
5. Toggle **Instrumental** ON
6. Click **Create**

### Prompt Tips for v5

- Keep under **500 characters** (Simple Mode limit)
- v5 supports conversational, detailed descriptions
- Include BPM, mood, instruments, and context
- Add "seamless loop" for loopable tracks
- Add "instrumental" to reinforce no vocals

---

## 1.2 BGM Tracks (7)

**How to use:** Paste entire prompt into **Song Description** field in Simple Mode. Toggle **Instrumental** ON.

---

### BGM_LOGIN
**ID:** `bgm_login`
**Scene:** LoginScene
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~80 BPM
**Context:** First screen the player sees, mysterious modern atmosphere

**Suno Prompt (Simple Mode):**
```
Dark atmospheric electronic, 80 BPM, ominous and foreboding, deep droning synth pads, oppressive low bass pulse, distant distorted Korean gayageum, fog-drenched dread, sinister urban ambience, Diablo-like dark menu atmosphere, haunting minor key melody, game login screen music, instrumental, seamless loop
```

---

### BGM_CHARACTER_SELECT
**ID:** `bgm_character_select`
**Scene:** CharacterSelectionScene
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~95 BPM
**Context:** Choosing between Laon (Warrior), Sian (Mage), Yujin (Archer)

**Suno Prompt (Simple Mode):**
```
Dark cinematic electronic, 95 BPM, ominous and tense, slow building minor key string pads, cold ambient synth drones, sparse haunting piano notes, oppressive atmosphere, shadowy character reveal, Diablo-like dark orchestral undertones, foreboding anticipation, gothic selection screen, instrumental, seamless loop
```

---

### BGM_TUTORIAL
**ID:** `bgm_tutorial`
**Scene:** Tutorial (Agency building — entertainment agency training floor)
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~130 BPM
**Context:** Hack & slash training in a modern office/agency environment

**Suno Prompt (Simple Mode):**
```
Dark driving electronic action, 130 BPM, fast-paced hack and slash, aggressive distorted synth bass, pounding snare hits, ominous sidechained drones, sinister urban combat, threatening beat drops, relentless and oppressive, cursed agency training floor, Diablo-like dungeon intensity, instrumental, seamless loop
```

---

### BGM_BILLAGE
**ID:** `bgm_billage`
**Scene:** Billage (Nightclub hub — safe zone)
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~110 BPM
**Context:** Nightclub safe haven with shops, NPCs, and preparation

**Suno Prompt (Simple Mode):**
```
Dark lo-fi Korean club beats, 110 BPM, dim neon nightclub atmosphere, murky R&B influenced synths, heavy deep house bass, shadowy and brooding, sinister lounge vibe, uneasy calm, tense safe zone with lurking danger underneath, cold urban nightlife, instrumental, seamless loop
```

---

### BGM_DUNGEON1
**ID:** `bgm_dungeon1`
**Scene:** Act1_1 (First underground dungeon floor)
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~140 BPM
**Context:** Fast-paced hack & slash in underground building floors, first encounter with demons

**Suno Prompt (Simple Mode):**
```
Aggressive dark industrial electronic, 140 BPM, crushing bass drops, distorted dissonant synth stabs, relentless pounding drums, suffocating underground tension, demonic presence dread, Diablo-like dungeon combat, gritty and menacing, savage hack and slash brutality, instrumental, seamless loop
```

---

### BGM_DUNGEON2
**ID:** `bgm_dungeon2`
**Scene:** Act1_2 (Deeper underground floor)
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~150 BPM
**Context:** Deeper descent, escalating danger, stronger demons

**Suno Prompt (Simple Mode):**
```
Brutal dark industrial electronic, 150 BPM, massive distorted bass, chaotic dissonant arpeggios, crushing relentless drum patterns, suffocating deeper descent into hell, overwhelming supernatural dread, Korean folklore horror, Diablo-like escalating terror, savage combat fury, instrumental, seamless loop
```

---

### BGM_BOSS
**ID:** `bgm_boss`
**Scene:** BossStage (Underground boss arena)
**Duration:** 2-3 minutes (loopable)
**Tempo:** ~160 BPM
**Context:** Epic boss fight against Dueoksini (Korean folklore demon), climactic showdown

**Suno Prompt (Simple Mode):**
```
Epic dark cinematic electronic, 160 BPM, massive ominous synth drops, thunderous war percussion, dissonant orchestral brass layered with crushing distorted bass, demonic boss battle, Diablo-like apocalyptic intensity, overwhelming dread and fury, desperate fight against ancient evil, instrumental, seamless loop
```

---

# Part 2: ElevenLabs SFX Prompts

## 2.1 ElevenLabs Interface Guide

### Accessing Sound Effects Generator

1. Go to https://elevenlabs.io/
2. Sign in / Create account
3. Navigate to **Playground** in sidebar
4. Select **Sound Effects**
5. Or go directly to: https://elevenlabs.io/app/sound-effects

### Interface Layout

| Setting | Description |
|---------|-------------|
| **Text Prompt** | Describe the sound (up to 450 chars) |
| **Duration** | Slider: Auto or 1-30 seconds |
| **Looping** | Toggle ON for seamless loops |
| **Prompt Influence** | Default 30%, higher = more precise |
| **Generate** | Creates 4 variations per generation |

### Recommended Settings for HYPERLINK

| Sound Type | Duration | Prompt Influence | Looping |
|------------|----------|------------------|---------|
| UI clicks/hovers | 0.2-0.5s | 85% | OFF |
| Combat hits | 0.3-0.5s | 85% | OFF |
| Skill cast sounds | 0.5-1.0s | 80% | OFF |
| Skill hit sounds | 0.3-0.6s | 80% | OFF |
| Enemy attacks | 0.3-0.6s | 80% | OFF |
| Boss sounds | 0.8-1.5s | 75% | OFF |
| Environmental | 0.3-1.0s | 75% | OFF |
| Element effects | 0.5-0.8s | 80% | OFF |

### Output & Download

- Each generation produces **4 variations** — listen to all before choosing
- Download as **WAV (48kHz)** for best quality
- Find previous generations in **History** tab

---

## 2.2 ElevenLabs Prompting Best Practices

### Golden Rule: Be Specific About the Actual Sound

Focus on concrete acoustic properties rather than abstract emotions.

**Bad (too abstract):**
```
Satisfying and powerful dark fantasy slash with K-Pop energy feeling
```

**Good (specific sound):**
```
Sharp metal sword slash whoosh, steel cutting air, brief high-frequency ring, fast attack swing
```

### Prompt Structure Formula

```
[Sound Source] + [Material/Texture] + [Action] + [Acoustic Properties] + [Environment]
```

**Example:**
```
Metal sword blade, steel on flesh, downward slash impact, sharp ring with low thud, indoor concrete room
```

### Key Audio Terminology

| Category | Terms |
|----------|-------|
| **Attack** | crisp, soft, sharp, punchy, sudden, gradual |
| **Texture** | metallic, synthetic, organic, crystalline, gritty |
| **Character** | bright, dark, warm, cold, hollow, resonant |
| **Duration** | staccato (short), sustained, fading, abrupt |
| **Effects** | reverb, dry, muffled, distorted, filtered |
| **Dynamics** | crescendo, diminuendo, impact, whoosh, drone |

### What Works Well

- Specific materials: "steel", "glass", "concrete", "flesh", "bone"
- Clear actions: "slash", "slam", "whoosh", "crack", "burst"
- Acoustic context: "indoor", "concrete hall", "muffled", "with reverb"
- Audio terms: "impact", "one-shot", "staccato", "sustained"
- Temporal markers: "brief", "sustained", "fading", "0.3 seconds"

### What to Avoid

- Abstract emotions: "satisfying", "powerful feeling", "K-Pop energy"
- Narrative descriptions: "the sound of a demon being vanquished"
- Multiple unrelated sounds in one prompt
- Long preambles: "Professional game audio, high-quality foley recording"
- Overly complex multi-layered descriptions

---

## 2.3 HYPERLINK Sound Palette

### Core Audio Identity

| Aspect | Sound Direction |
|--------|-----------------|
| **Overall** | Modern, urban, electronic, dark-edged |
| **UI** | High-tech glass/neon, clean clicks, bright confirmations |
| **Combat** | Fast metallic impacts, aggressive slashes, visceral hits |
| **Skills** | Supernatural energy bursts, elemental effects |
| **Environment** | Urban interior reverb, concrete/metal surfaces |
| **Enemies** | Monstrous, demonic, otherworldly |

### Material References for HYPERLINK

| Element | Materials | Texture Words |
|---------|-----------|---------------|
| **UI** | Glass, neon, digital | Crisp, bright, clean |
| **Player Combat** | Steel, metal, flesh | Sharp, impactful, fast |
| **Skills** | Energy, electricity, supernatural | Crackling, surging, pulsing |
| **Enemies** | Bone, dark matter, demonic flesh | Heavy, guttural, menacing |
| **Environment** | Concrete, steel doors, neon | Reverberant, urban, industrial |

### Environment Context

Most HYPERLINK sounds should suggest a **modern urban interior** with:
- Light indoor reverb (0.2-0.4s decay)
- Concrete/industrial acoustic character
- Modern, slightly cold atmosphere

---

## 2.4 Player Combat SFX (4)

> **GameSoundLibrary mapping:** These map directly to the 4 player sound properties.

---

### PLAYER_BASIC_ATTACK
**ID:** `player_basic_attack`
**Property:** `GameSoundLibrary.PlayerBasicAttack`
**Duration:** 0.4 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Sharp metal sword slash whoosh, steel blade cutting air, fast attack swing, brief high-frequency ring with low thud, indoor room
```

---

### PLAYER_HIT
**ID:** `player_hit`
**Property:** `GameSoundLibrary.PlayerHit`
**Duration:** 0.3 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Flesh impact punch, meaty body hit with fabric rustle, painful thud, brief grunt-like compression, indoor concrete reverb
```

---

### PLAYER_DEATH
**ID:** `player_death`
**Property:** `GameSoundLibrary.PlayerDeath`
**Duration:** 0.8 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Heavy body collapse on concrete floor, armor clattering, metallic equipment dropping, low hollow thud fading to silence
```

---

### PLAYER_SKILL_CAST
**ID:** `player_skill_cast`
**Property:** `GameSoundLibrary.PlayerSkillCast`
**Duration:** 0.5 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Supernatural energy charging burst, electric power surge whoosh, bright ascending synth tone, magical activation crackling, quick release
```

---

## 2.5 Enemy Combat SFX (10)

> **GameSoundLibrary mapping:** These map directly to the 10 enemy sound properties.

---

### ENEMY_MELEE_ATTACK
**ID:** `enemy_melee_attack`
**Property:** `GameSoundLibrary.EnemyMeleeAttack`
**Duration:** 0.4 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Heavy claw swipe whoosh, aggressive melee swing, sharp air displacement, bestial attack motion, menacing swoosh with brief impact
```

---

### ENEMY_RANGED_ATTACK
**ID:** `enemy_ranged_attack`
**Property:** `GameSoundLibrary.EnemyRangedAttack`
**Duration:** 0.5 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Dark energy projectile launch, distorted bolt firing, buzzing magical missile whoosh, supernatural ranged shot with fading trail
```

---

### ENEMY_HIT
**ID:** `enemy_hit`
**Property:** `GameSoundLibrary.EnemyHit`
**Duration:** 0.3 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Monster flesh impact, wet creature hit thud, dark organic body strike, brief splatter with low grunt
```

---

### ENEMY_DEATH
**ID:** `enemy_death`
**Property:** `GameSoundLibrary.EnemyDeath`
**Duration:** 0.7 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Creature dissolving into particles, dark matter dispersal whoosh, demonic entity fading away, hollow disintegration hiss
```

---

### EPIC_SPAWN
**ID:** `epic_spawn`
**Property:** `GameSoundLibrary.EpicSpawn`
**Duration:** 1.2 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Powerful entity materializing, dark energy coalescing with deep rumble, ominous bass surge building, heavy supernatural presence arriving, dramatic reveal
```

---

### EPIC_SPECIAL_ATTACK
**ID:** `epic_special_attack`
**Property:** `GameSoundLibrary.EpicSpecialAttack`
**Duration:** 0.8 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Massive monster special attack, dark energy explosion burst, heavy supernatural shockwave, distorted power slam with deep bass impact
```

---

### BOSS_SPAWN
**ID:** `boss_spawn`
**Property:** `GameSoundLibrary.BossSpawn`
**Duration:** 1.5 seconds
**Prompt Influence:** 70%

**ElevenLabs Prompt:**
```
Massive demonic entity emerging, ground trembling deep rumble, overwhelming dark presence manifesting, ominous bass drone building to thunderous reveal, reality distorting crackle
```

---

### BOSS_ATTACK
**ID:** `boss_attack`
**Property:** `GameSoundLibrary.BossAttack`
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Colossal creature strike, heavy bone-crushing melee swing, massive claw slam with ground crack, thunderous impact, deep bass thud
```

---

### BOSS_SPECIAL_ATTACK
**ID:** `boss_special_attack`
**Property:** `GameSoundLibrary.BossSpecialAttack`
**Duration:** 1.0 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Devastating boss power attack, dark supernatural explosion, massive energy shockwave with ground tremor, distorted demonic force blast, heavy reverberating impact
```

---

### BOSS_DEATH
**ID:** `boss_death`
**Property:** `GameSoundLibrary.BossDeath`
**Duration:** 1.5 seconds
**Prompt Influence:** 70%

**ElevenLabs Prompt:**
```
Massive entity collapsing, bone structure crumbling with echoing cracks, dark energy dissipating in waves, deep rumbling fade, hollow void implosion to silence
```

---

## 2.6 UI SFX (6)

> **GameSoundLibrary mapping:** These map directly to the 6 UI sound properties.

---

### UI_CLICK
**ID:** `ui_click`
**Property:** `GameSoundLibrary.UIClick`
**Duration:** 0.2 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Crisp glass button click, bright digital tap, clean high-tech confirmation ping, brief neon-like ring
```

---

### UI_HOVER
**ID:** `ui_hover`
**Property:** `GameSoundLibrary.UIHover`
**Duration:** 0.15 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Soft digital hover chime, very quiet glass shimmer, gentle high-frequency tick, subtle neon glow sound
```

---

### ITEM_PICKUP
**ID:** `item_pickup`
**Property:** `GameSoundLibrary.ItemPickup`
**Duration:** 0.4 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Item collected chime, ascending two-note digital ping, metallic object pickup, brief bright confirmation tone with soft sparkle
```

---

### ITEM_EQUIP
**ID:** `item_equip`
**Property:** `GameSoundLibrary.ItemEquip`
**Duration:** 0.5 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Armor piece locking into place, metallic snap click, equipment slot engaging, mechanical latching sound with brief resonant ring
```

---

### LEVEL_UP
**ID:** `level_up`
**Property:** `GameSoundLibrary.LevelUp`
**Duration:** 1.0 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Power-up fanfare burst, ascending bright synth chimes, energy surge crescendo, triumphant electronic stinger, sparkling high tones with warm bass swell
```

---

### SKILL_UNLOCK
**ID:** `skill_unlock`
**Property:** `GameSoundLibrary.SkillUnlock`
**Duration:** 0.8 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Skill node activating, electric circuit connecting snap, bright energy pulse outward, digital unlock confirmation with ascending crystalline tones
```

---

## 2.7 Environmental SFX (3)

> **GameSoundLibrary mapping:** These map directly to the 3 environment sound properties.

---

### FOOTSTEP
**ID:** `footstep`
**Property:** `GameSoundLibrary.Footstep`
**Duration:** 0.3 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Single footstep on concrete floor, shoe sole tap on hard surface, indoor step with light reverb, clean and dry
```

---

### DOOR_OPEN
**ID:** `door_open`
**Property:** `GameSoundLibrary.DoorOpen`
**Duration:** 0.8 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Heavy metal door sliding open, motorized mechanism hum, industrial door panel moving, steel on steel friction, modern automatic door
```

---

### PORTAL_USE
**ID:** `portal_use`
**Property:** `GameSoundLibrary.PortalUse`
**Duration:** 1.0 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Teleport portal activation, warping energy vortex whoosh, spatial distortion crackle, bright electric surge passing through, reality shifting shimmer fading out
```

---

## 2.8 Five Elements SFX (5)

> **SpecialAttackBase subclasses:** SA_Fire, SA_Water, SA_Earth, SA_Wood, SA_Metal. Used by enemies with elemental special attacks.

---

### ELEMENT_FIRE
**ID:** `element_fire`
**Element:** SA_Fire (DoT — damage over time, burn ignition)
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Fire igniting burst, crackling flames spreading, intense heat sizzle, burning ember particles scattering, sharp fire whoosh
```

---

### ELEMENT_WATER
**ID:** `element_water`
**Element:** SA_Water (freeze/slow effect)
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Ice crystallizing crack, frost spreading snap, frozen surface forming, sharp cold crackle, brittle ice shattering ring
```

---

### ELEMENT_EARTH
**ID:** `element_earth`
**Element:** SA_Earth (blind/silence effect)
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Ground tremor rumble, earth cracking apart, heavy stone debris shifting, deep bass vibration, concrete splitting impact
```

---

### ELEMENT_WOOD
**ID:** `element_wood`
**Element:** SA_Wood (root/defense debuff)
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Vine roots bursting from ground, wood creaking and cracking rapidly, organic growth eruption, tangling branches snapping into place
```

---

### ELEMENT_METAL
**ID:** `element_metal`
**Element:** SA_Metal (knockback/stun)
**Duration:** 0.5 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Heavy metal impact clang, steel collision ring, sharp metallic knockback hit, reverberating iron bell strike, staccato
```

---

## 2.9 Raon (Laon) Skill Sounds (12)

> **SkillData fields:** Each skill has `SkillCastSound` and `SkillHitSound`. Laon is a Warrior/Strength class wielding a large blade.

### Cast Sounds (6)

---

### SKILL_JUDGMENT_CAST
**ID:** `skill_judgment_cast`
**Skill:** Judgment (심판) — heavy downward slam
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Heavy blade raising overhead, metal weapon whoosh upward, powerful sword lift with air displacement, building energy tension before downward strike
```

---

### SKILL_SWIFT_SLASH_CAST
**ID:** `skill_swift_slash_cast`
**Skill:** Swift Slash (쾌속 가르기) — fast dash forward
**Duration:** 0.4 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Fast forward dash whoosh, rapid air displacement rush, speed burst wind tunnel, sharp acceleration swoosh, brief and snappy
```

---

### SKILL_CONVICTION_CAST
**ID:** `skill_conviction_cast`
**Skill:** Conviction (단죄) — leap into ground slam
**Duration:** 0.7 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Powerful upward leap whoosh, body launching into air, ascending wind rush, heavy warrior jumping with armor rattle, building momentum
```

---

### SKILL_EXECUTIONERS_BLADE_CAST
**ID:** `skill_executioners_blade_cast`
**Skill:** Executioner's Blade (소멸검) — channeled energy strike
**Duration:** 0.8 seconds
**Prompt Influence:** 75%

**ElevenLabs Prompt:**
```
Dark energy channeling into blade, supernatural power charging, low humming vibration building, ominous electric crackling intensifying, soul-infused weapon resonance
```

---

### SKILL_PIERCING_THRUST_CAST
**ID:** `skill_piercing_thrust_cast`
**Skill:** Piercing Thrust (영혼 관통) — forward thrust
**Duration:** 0.5 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Sharp forward thrust lunge, blade piercing air, fast spear-like stab whoosh, concentrated point strike, penetrating wind burst
```

---

### SKILL_WHIRLWIND_CAST
**ID:** `skill_whirlwind_cast`
**Skill:** Whirlwind of Faith (신념의 회오리) — spinning blade attack
**Duration:** 1.0 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Spinning blade whirlwind, rapid circular metal whoosh, sustained tornado-like wind rush, rotating steel cutting air repeatedly, escalating spin speed
```

---

### Hit Sounds (6)

---

### SKILL_JUDGMENT_HIT
**ID:** `skill_judgment_hit`
**Skill:** Judgment (심판) — shockwave ground crack
**Duration:** 0.5 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Heavy blade slamming ground, concrete cracking shockwave, powerful downward impact with ground tremor, debris scattering burst
```

---

### SKILL_SWIFT_SLASH_HIT
**ID:** `skill_swift_slash_hit`
**Skill:** Swift Slash (쾌속 가르기) — cutting slice impact
**Duration:** 0.3 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Sharp blade cutting through flesh, clean fast slice impact, steel through material snap, brief wet cut with metallic ring
```

---

### SKILL_CONVICTION_HIT
**ID:** `skill_conviction_hit`
**Skill:** Conviction (단죄) — AoE ground slam explosion
**Duration:** 0.7 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Massive ground slam explosion, body crashing down with AoE shockwave, concrete shattering outward, heavy tremor impact with debris blast
```

---

### SKILL_EXECUTIONERS_BLADE_HIT
**ID:** `skill_executioners_blade_hit`
**Skill:** Executioner's Blade (소멸검) — soul-piercing strike
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Dark energy blade piercing through, supernatural penetration impact, soul extraction whoosh, distorted deep slash with ethereal echo
```

---

### SKILL_PIERCING_THRUST_HIT
**ID:** `skill_piercing_thrust_hit`
**Skill:** Piercing Thrust (영혼 관통) — penetrating soul impact
**Duration:** 0.4 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Blade stabbing through target, sharp penetration impact, steel through bone thud, soul energy release burst on contact
```

---

### SKILL_WHIRLWIND_HIT
**ID:** `skill_whirlwind_hit`
**Skill:** Whirlwind of Faith (신념의 회오리) — multi-hit spin
**Duration:** 0.3 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Spinning blade slash impact, rapid metal cutting hit, fast rotational strike thud, brief sharp whirlwind contact
```

---

## 2.10 Additional SFX (3)

---

### RED_SODA_DRINK
**ID:** `red_soda_drink`
**Context:** Health potion (Red Soda) consumption
**Duration:** 0.6 seconds
**Prompt Influence:** 80%

**ElevenLabs Prompt:**
```
Soda can opening pop fizz, carbonated drink gulp, bubbly liquid consumption, refreshing fizz burst with swallowing sound
```

---

### GOLD_PICKUP
**ID:** `gold_pickup`
**Context:** Gold currency collected from enemies/ground
**Duration:** 0.3 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Coins clinking together, metallic gold pieces collected, bright currency jingle, brief shiny coin impact ring
```

---

### CRITICAL_HIT
**ID:** `critical_hit`
**Context:** Critical damage multiplier triggered
**Duration:** 0.5 seconds
**Prompt Influence:** 85%

**ElevenLabs Prompt:**
```
Devastating crushing blow, bone cracking heavy impact with electric burst, powerful shockwave slam, amplified visceral strike with bright flash ring
```

---

# Part 3: File Organization & Import Settings

## 3.1 Output Folder Structure

```
Assets/_ProjectHYPERLINK/AudioClip/
├── BGM/                          # Suno v5 (2:00+ tracks)
│   ├── bgm_login.mp3
│   ├── bgm_character_select.mp3
│   ├── bgm_tutorial.mp3
│   ├── bgm_billage.mp3
│   ├── bgm_dungeon1.mp3
│   ├── bgm_dungeon2.mp3
│   └── bgm_boss.mp3
├── Player/                       # Player combat sounds
│   ├── player_basic_attack.wav
│   ├── player_hit.wav
│   ├── player_death.wav
│   └── player_skill_cast.wav
├── Enemy/                        # Enemy & Boss sounds
│   ├── enemy_melee_attack.wav
│   ├── enemy_ranged_attack.wav
│   ├── enemy_hit.wav
│   ├── enemy_death.wav
│   ├── epic_spawn.wav
│   ├── epic_special_attack.wav
│   ├── boss_spawn.wav
│   ├── boss_attack.wav
│   ├── boss_special_attack.wav
│   └── boss_death.wav
├── UI/                           # UI interaction sounds
│   ├── ui_click.wav
│   ├── ui_hover.wav
│   ├── item_pickup.wav
│   ├── item_equip.wav
│   ├── level_up.wav
│   └── skill_unlock.wav
├── Environment/                  # Environmental sounds
│   ├── footstep.wav
│   ├── door_open.wav
│   └── portal_use.wav
├── Elements/                     # Five element effects
│   ├── element_fire.wav
│   ├── element_water.wav
│   ├── element_earth.wav
│   ├── element_wood.wav
│   └── element_metal.wav
├── Skills/                       # Per-skill sounds (SkillData fields)
│   └── Laon/
│       ├── skill_judgment_cast.wav
│       ├── skill_judgment_hit.wav
│       ├── skill_swift_slash_cast.wav
│       ├── skill_swift_slash_hit.wav
│       ├── skill_conviction_cast.wav
│       ├── skill_conviction_hit.wav
│       ├── skill_executioners_blade_cast.wav
│       ├── skill_executioners_blade_hit.wav
│       ├── skill_piercing_thrust_cast.wav
│       ├── skill_piercing_thrust_hit.wav
│       ├── skill_whirlwind_cast.wav
│       └── skill_whirlwind_hit.wav
└── Misc/                         # Additional sounds
    ├── red_soda_drink.wav
    ├── gold_pickup.wav
    └── critical_hit.wav
```

## 3.2 Unity Import Settings

| Category | Load Type | Compression | Quality | Sample Rate | Notes |
|----------|-----------|-------------|---------|-------------|-------|
| **BGM** (Suno) | Streaming | Vorbis | 70% | 44100 Hz | Loop: ON, reduces memory |
| **Player SFX** | Decompress on Load | PCM | — | Original | Short clips, instant playback |
| **Enemy SFX** | Compressed in Memory | Vorbis | 85% | Original | Balance of quality/memory |
| **Boss SFX** | Compressed in Memory | Vorbis | 80% | Original | Slightly longer clips |
| **UI SFX** | Decompress on Load | PCM | — | Original | Lowest latency for UI |
| **Environment** | Decompress on Load | ADPCM | — | Original | Short, frequent clips |
| **Element SFX** | Compressed in Memory | Vorbis | 85% | Original | Medium frequency |
| **Skill Sounds** | Compressed in Memory | Vorbis | 85% | Original | Per-SkillData assignment |
| **Misc SFX** | Decompress on Load | PCM | — | Original | Short one-shots |

## 3.3 GameSoundLibrary Property → File Mapping

After generating audio, assign clips in the `GameSoundLibrary` ScriptableObject Inspector:

| GameSoundLibrary Property | Audio File |
|---------------------------|------------|
| `PlayerBasicAttack` | `Player/player_basic_attack.wav` |
| `PlayerHit` | `Player/player_hit.wav` |
| `PlayerDeath` | `Player/player_death.wav` |
| `PlayerSkillCast` | `Player/player_skill_cast.wav` |
| `EnemyMeleeAttack` | `Enemy/enemy_melee_attack.wav` |
| `EnemyRangedAttack` | `Enemy/enemy_ranged_attack.wav` |
| `EnemyHit` | `Enemy/enemy_hit.wav` |
| `EnemyDeath` | `Enemy/enemy_death.wav` |
| `EpicSpawn` | `Enemy/epic_spawn.wav` |
| `EpicSpecialAttack` | `Enemy/epic_special_attack.wav` |
| `BossSpawn` | `Enemy/boss_spawn.wav` |
| `BossAttack` | `Enemy/boss_attack.wav` |
| `BossSpecialAttack` | `Enemy/boss_special_attack.wav` |
| `BossDeath` | `Enemy/boss_death.wav` |
| `UIClick` | `UI/ui_click.wav` |
| `UIHover` | `UI/ui_hover.wav` |
| `ItemPickup` | `UI/item_pickup.wav` |
| `ItemEquip` | `UI/item_equip.wav` |
| `LevelUp` | `UI/level_up.wav` |
| `SkillUnlock` | `UI/skill_unlock.wav` |
| `Footstep` | `Environment/footstep.wav` |
| `DoorOpen` | `Environment/door_open.wav` |
| `PortalUse` | `Environment/portal_use.wav` |

**Per-Skill Sounds (SkillData Inspector):**

| Skill Asset | CastSound File | HitSound File |
|-------------|----------------|---------------|
| `Judgement.asset` | `Skills/Laon/skill_judgment_cast.wav` | `Skills/Laon/skill_judgment_hit.wav` |
| `SwiftSlash.asset` | `Skills/Laon/skill_swift_slash_cast.wav` | `Skills/Laon/skill_swift_slash_hit.wav` |
| `Conviction.asset` | `Skills/Laon/skill_conviction_cast.wav` | `Skills/Laon/skill_conviction_hit.wav` |
| `ExecutionersBlade.asset` | `Skills/Laon/skill_executioners_blade_cast.wav` | `Skills/Laon/skill_executioners_blade_hit.wav` |
| `PiercingThrust.asset` | `Skills/Laon/skill_piercing_thrust_cast.wav` | `Skills/Laon/skill_piercing_thrust_hit.wav` |
| `WhirlwindOfFaith.asset` | `Skills/Laon/skill_whirlwind_cast.wav` | `Skills/Laon/skill_whirlwind_hit.wav` |

**Scene BGM (SceneBGMController Inspector):**

| Scene | BGM File |
|-------|----------|
| LoginScene | `BGM/bgm_login.mp3` |
| CharacterSelectionScene | `BGM/bgm_character_select.mp3` |
| Tutorial | `BGM/bgm_tutorial.mp3` |
| Billage | `BGM/bgm_billage.mp3` |
| Act1_1 | `BGM/bgm_dungeon1.mp3` |
| Act1_2 | `BGM/bgm_dungeon2.mp3` |
| BossStage | `BGM/bgm_boss.mp3` |

---

# Part 4: Generation Workflow & Checklist

## 4.1 Suno v5 Step-by-Step Workflow

1. Go to https://suno.com/
2. Click **Create** (left sidebar)
3. Select **Simple** tab
4. Verify model is **v5** (dropdown)
5. Paste the full prompt from Part 1 into **Song Description**
6. Toggle **Instrumental** ON
7. Click **Create**
8. Wait for generation (~30-60 seconds)
9. Listen to all generated variants and pick the best
10. Click **...** menu → **Download** → **Audio**
11. Rename file to match ID (e.g., `bgm_login.mp3`)
12. Place in `Assets/_ProjectHYPERLINK/AudioClip/BGM/`
13. Set Unity import settings per Section 3.2

### Suno Tips

- Each generation creates 2 song variants — listen to both
- For loops: Suno doesn't guarantee seamless loops — post-process in Audacity
- v5 can generate up to 4 minutes per track
- If a prompt doesn't produce good results, try slight rewording or different instrument emphasis
- Generate multiple times and pick the best across batches

---

## 4.2 ElevenLabs Step-by-Step Workflow

1. Go to https://elevenlabs.io/app/sound-effects
2. Paste the prompt from Part 2 into the text area
3. Set **Duration** using the slider (match the spec for each sound)
4. Set **Prompt Influence** as specified (typically 75-85%)
5. Leave **Looping** OFF (all HYPERLINK SFX are one-shots)
6. Click **Generate**
7. Listen to all **4 variations**
8. Click download icon on the best one
9. Choose **WAV** format (48kHz) for best quality
10. Rename file to match ID (e.g., `player_basic_attack.wav`)
11. Place in the appropriate subfolder per Section 3.1
12. Set Unity import settings per Section 3.2

### ElevenLabs Tips

- Each generation creates 4 variations — always listen to all
- For very short sounds (<0.3s): Set duration precisely, higher prompt influence
- History tab: Access all previous generations for re-download
- If results are too abstract: increase prompt influence to 90%
- If results are too repetitive: decrease prompt influence to 70%

---

## 4.3 Post-Processing Tips

### For BGM Loops (Audacity)

1. Import the Suno-generated track
2. Find natural loop points (typically at phrase boundaries)
3. Trim to loop-friendly length
4. Apply small crossfade at loop boundary (50-100ms)
5. Export as OGG Vorbis (Quality 7) or MP3 (192kbps)
6. Test in Unity with AudioSource Loop enabled

### For SFX

1. Import WAV into Audacity
2. **Trim** silence from start and end (leave ~10ms at start)
3. **Normalize** volume to -1 dB
4. Apply gentle **fade-out** if needed (last 20-50ms)
5. Export as WAV (16-bit, original sample rate)
6. Keep file sizes small: <200KB for short SFX, <500KB for longer sounds

### Volume Balancing Guidelines

| Category | Target Volume (relative) |
|----------|--------------------------|
| BGM | -12 dB (background) |
| Player Attack SFX | -3 dB (prominent) |
| Enemy Attack SFX | -6 dB (noticeable) |
| Hit/Impact SFX | -1 dB (punchy) |
| UI SFX | -6 dB (clean, not intrusive) |
| Environmental | -9 dB (ambient) |
| Skill Cast SFX | -3 dB (prominent) |
| Skill Hit SFX | -1 dB (impactful) |

---

## 4.4 Full Asset Checklist

### BGM — Suno v5 (7 tracks)

- [ ] `bgm_login` — LoginScene atmospheric intro
- [ ] `bgm_character_select` — CharacterSelectionScene stylish K-Pop
- [ ] `bgm_tutorial` — Tutorial agency hack & slash
- [ ] `bgm_billage` — Billage nightclub chill hub
- [ ] `bgm_dungeon1` — Act1_1 aggressive underground
- [ ] `bgm_dungeon2` — Act1_2 heavy deeper floor
- [ ] `bgm_boss` — BossStage epic boss fight

### Player Combat — ElevenLabs (4 sounds)

- [ ] `player_basic_attack` → GameSoundLibrary.PlayerBasicAttack
- [ ] `player_hit` → GameSoundLibrary.PlayerHit
- [ ] `player_death` → GameSoundLibrary.PlayerDeath
- [ ] `player_skill_cast` → GameSoundLibrary.PlayerSkillCast

### Enemy Combat — ElevenLabs (10 sounds)

- [ ] `enemy_melee_attack` → GameSoundLibrary.EnemyMeleeAttack
- [ ] `enemy_ranged_attack` → GameSoundLibrary.EnemyRangedAttack
- [ ] `enemy_hit` → GameSoundLibrary.EnemyHit
- [ ] `enemy_death` → GameSoundLibrary.EnemyDeath
- [ ] `epic_spawn` → GameSoundLibrary.EpicSpawn
- [ ] `epic_special_attack` → GameSoundLibrary.EpicSpecialAttack
- [ ] `boss_spawn` → GameSoundLibrary.BossSpawn
- [ ] `boss_attack` → GameSoundLibrary.BossAttack
- [ ] `boss_special_attack` → GameSoundLibrary.BossSpecialAttack
- [ ] `boss_death` → GameSoundLibrary.BossDeath

### UI — ElevenLabs (6 sounds)

- [ ] `ui_click` → GameSoundLibrary.UIClick
- [ ] `ui_hover` → GameSoundLibrary.UIHover
- [ ] `item_pickup` → GameSoundLibrary.ItemPickup
- [ ] `item_equip` → GameSoundLibrary.ItemEquip
- [ ] `level_up` → GameSoundLibrary.LevelUp
- [ ] `skill_unlock` → GameSoundLibrary.SkillUnlock

### Environmental — ElevenLabs (3 sounds)

- [ ] `footstep` → GameSoundLibrary.Footstep
- [ ] `door_open` → GameSoundLibrary.DoorOpen
- [ ] `portal_use` → GameSoundLibrary.PortalUse

### Five Elements — ElevenLabs (5 sounds)

- [ ] `element_fire` — SA_Fire: burn ignition / DoT
- [ ] `element_water` — SA_Water: freeze / ice crack
- [ ] `element_earth` — SA_Earth: tremor / rumble
- [ ] `element_wood` — SA_Wood: vine / root burst
- [ ] `element_metal` — SA_Metal: clang / knockback impact

### Raon Skill Cast Sounds — ElevenLabs (6 sounds)

- [ ] `skill_judgment_cast` — Judgment (심판): heavy blade raise
- [ ] `skill_swift_slash_cast` — Swift Slash (쾌속 가르기): dash whoosh
- [ ] `skill_conviction_cast` — Conviction (단죄): leap jump
- [ ] `skill_executioners_blade_cast` — Executioner's Blade (소멸검): energy channel
- [ ] `skill_piercing_thrust_cast` — Piercing Thrust (영혼 관통): forward lunge
- [ ] `skill_whirlwind_cast` — Whirlwind of Faith (신념의 회오리): spin start

### Raon Skill Hit Sounds — ElevenLabs (6 sounds)

- [ ] `skill_judgment_hit` — Judgment: ground shockwave crack
- [ ] `skill_swift_slash_hit` — Swift Slash: cutting slice impact
- [ ] `skill_conviction_hit` — Conviction: AoE ground slam explosion
- [ ] `skill_executioners_blade_hit` — Executioner's Blade: soul-piercing strike
- [ ] `skill_piercing_thrust_hit` — Piercing Thrust: penetrating thrust impact
- [ ] `skill_whirlwind_hit` — Whirlwind of Faith: spinning blade contact

### Additional — ElevenLabs (3 sounds)

- [ ] `red_soda_drink` — Health potion consumption
- [ ] `gold_pickup` — Gold currency collected
- [ ] `critical_hit` — Critical damage multiplier

---

## Summary

**Total Audio Assets: 50 files**

| Category | Count | Tool | Duration Range |
|----------|-------|------|----------------|
| BGM Music | 7 | Suno v5 | 2-3 minutes |
| Player Combat SFX | 4 | ElevenLabs | 0.3-0.8s |
| Enemy Combat SFX | 10 | ElevenLabs | 0.3-1.5s |
| UI SFX | 6 | ElevenLabs | 0.15-1.0s |
| Environmental SFX | 3 | ElevenLabs | 0.3-1.0s |
| Five Element SFX | 5 | ElevenLabs | 0.5-0.6s |
| Raon Skill Cast | 6 | ElevenLabs | 0.4-1.0s |
| Raon Skill Hit | 6 | ElevenLabs | 0.3-0.7s |
| Additional SFX | 3 | ElevenLabs | 0.3-0.6s |

**Estimated Generation Time:**
- Suno BGM tracks (7): ~1 hour (with variant selection)
- ElevenLabs SFX (43): ~3-4 hours (with variant selection)
- Post-processing: ~1-2 hours
- **Total: ~5-7 hours**

### Future Expansion

When Sian (Mage) and Yujin (Archer) skills are ready, add their cast/hit sound prompts to Section 2.9 following the same pattern. Create subfolders `Skills/Sian/` and `Skills/Yujin/` under the AudioClip directory.

---

*Generated for HYPERLINK by 333Percent. Audio direction: Modern electronic/synth + K-Pop influences + dark edgy undertones.*
