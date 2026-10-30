# Binary4Babies Roadmap

> **Big ideas. Tiny bits.**

## Vision

Binary4Babies is a playground for learning computing by playing with computing.

The objective is not to take conventional lessons and disguise them as games.

The objective is to make the computing concept itself part of the game mechanic.

```text
PLAY
 ↓
EXPERIMENT
 ↓
NOTICE A PATTERN
 ↓
UNDERSTAND THE MECHANIC
 ↓
DISCOVER THE CONCEPT
 ↓
LEARN ITS NAME
 ↓
"I GET IT!"
```

The player should ideally understand an idea before they are expected to know the terminology used to describe it.

---

# The Golden Rule

> **Mechanic first. Terminology second.**

If we need to explain the lesson before the player can enjoy the game, redesign the game.

Binary4Babies should prefer:

```text
DISCOVERY
over
INSTRUCTION

PLAY
over
LECTURE

EXPERIMENTATION
over
MEMORISATION

CURIOSITY
over
CURRICULUM

UNDERSTANDING
over
JARGON
```

The games should be enjoyable even when the player does not realise they are learning anything.

---

# The Beginner Is Not the Problem

Binary4Babies should never treat lack of knowledge as lack of intelligence.

A player who does not know what a bit is has not failed.

They simply have not encountered the idea yet.

The game's job is to create that encounter.

```text
UNKNOWN
   ↓
PLAY WITH IT
   ↓
FAMILIAR
   ↓
UNDERSTOOD
   ↓
NAMED
```

This is the spirit behind the "for Babies" joke.

The subject can be complicated.

The introduction does not have to be.

---

# Product Shape

Binary4Babies should not become one enormous educational game.

It should become a collection of small, focused games.

```text
BINARY4BABIES
      │
      ├── BitPop
      ├── DuoBits
      ├── TriToggle
      ├── NibBall
      ├── ByteBounce
      ├── Count-0-1
      ├── Flip & Fit
      └── Future Games
```

Each game should explore one or two ideas well.

A player should be able to launch a game because it looks entertaining, without first understanding where it sits in a curriculum.

---

# Learning Progression

There may be a natural conceptual progression:

```text
STATE
  ↓
BITS
  ↓
BINARY
  ↓
GROUPS OF BITS
  ↓
NUMBERS
  ↓
CHARACTERS
  ↓
LOGIC
  ↓
MEMORY
  ↓
FILES
  ↓
STORAGE
  ↓
PROGRAMS
  ↓
NETWORKS
  ↓
SYSTEMS
```

This is a map of possible concepts.

It should not become a compulsory linear course.

Players should be free to explore.

---

# Phase 0: Prove the Idea

Before building a suite, prove that the teaching philosophy actually works.

The first milestone is not:

> Build an educational platform.

It is:

> **Build one tiny game that is fun and accidentally teaches something.**

That game is NibBall.

---

# Phase 1: NibBall MVP

> **A whole game in 4 bits.**

NibBall is the first proof of the Binary4Babies concept.

The game should revolve around four binary states.

Conceptually:

```text
0000
0001
0010
0011
0100
0101
0110
0111
1000
1001
1010
1011
1100
1101
1110
1111
```

The player should not need to understand that representation before playing.

They should initially encounter something visual and interactive.

For example:

```text
BALL

┌───┬───┬───┬───┐
│ ○ │ ● │ ○ │ ● │
└───┴───┴───┴───┘
```

The four states affect gameplay.

The player experiments.

Patterns emerge.

Eventually the game reveals:

```text
○ = 0
● = 1
```

and:

> **Four bits together are called a nibble.**

The terminology arrives after the experience.

---

# NibBall MVP Requirements

Keep the first version extremely small.

It needs:

- a playable objective
- four meaningful binary states
- visible state changes
- consequences for those state changes
- enough feedback for patterns to emerge
- a reveal of the binary representation
- a reveal of the word "nibble"

It does not initially need:

- accounts
- cloud saves
- multiplayer
- elaborate progression
- complex graphics
- a curriculum engine
- achievements
- professional integration
- VNA
- FileCity
- Charkive
- BoreDOOM!

## Success Criterion

> A player can enjoy NibBall without knowing what a nibble is, and understand what a nibble is better after playing it.

That proves the central Binary4Babies idea.

---

# Phase 2: BitPop

Once NibBall proves the concept, reduce computing to its smallest useful abstraction.

One bit.

```text
0
1

OFF
ON

FALSE
TRUE

NO
YES
```

But do not initially teach those equivalences as a list.

Give the player one thing with two possible states.

Make changing that state fun.

Let the player discover:

> This thing has exactly two possibilities.

Only afterwards introduce:

```text
0
1
```

and finally:

> **That's a bit.**

## Success Criterion

> The player understands binary state through interaction rather than definition.

---

# Phase 3: Two and Three Bits

Games such as DuoBits and TriToggle can introduce the combinatorial power of adding bits.

The point is not merely to tell the player:

```text
1 bit  = 2 states
2 bits = 4 states
3 bits = 8 states
4 bits = 16 states
```

The player should experience the expansion.

```text
1 switch
    ↓
simple

2 switches
    ↓
more possibilities

3 switches
    ↓
wait... there are loads of these

4 switches
    ↓
NIBBLE
```

The discovery should create the question before the explanation supplies the answer.

---

# Phase 4: Binary Counting

Introduce numerical meaning only after binary state feels natural.

A game such as Count-0-1 can gradually reveal that bit patterns can represent numbers.

The progression might conceptually move through:

```text
0000
0001
0010
0011
0100
...
```

But the game should teach this through manipulation and consequence rather than requiring memorisation of a conversion table.

## Success Criterion

> The player begins predicting what the next binary state will be.

---

# Phase 5: ByteBounce

Once four bits feel understandable, introduce eight.

```text
NIBBLE
4 bits

        ↓

BYTE
8 bits
```

ByteBounce should make an eight-bit quantity feel like an extension of something the player already understands.

The objective is not:

> Memorise that there are eight bits in a byte.

The objective is:

> Give the player a reason to want more than four bits.

Then give them four more.

Finally:

> **That's a byte.**

---

# Phase 6: Logic

Introduce logic through consequences.

Possible game:

> **Flip & Fit**

Avoid beginning with truth tables.

Start with mechanisms.

```text
SWITCH A ----\
              AND ----> DOOR
SWITCH B ----/
```

The player discovers:

```text
A alone:
no

B alone:
no

A + B:
YES
```

Then give the relationship its name:

> **AND**

Repeat with concepts such as:

```text
OR
NOT
XOR
```

only where each operation supports a genuinely understandable game mechanic.

Truth tables can appear later as a notation describing something the player already knows.

---

# Phase 7: Memory

Create a game in which remembering state becomes useful.

Do not initially teach:

> Computers contain memory which stores state.

Instead create the problem:

```text
Something happened.

The game needs to remember that it happened.

How?
```

The player should encounter the need for memory before the formal concept.

The game can then introduce ideas such as:

```text
remember
forget
read
write
state
```

and later connect those ideas to computer memory.

---

# Phase 8: Storage

Memory naturally leads to a new problem:

> What if I want to keep something?

Give the player something worth keeping.

Then create scarcity or persistence.

```text
OBJECT
   ↓
KEEP IT
   ↓
WHERE?
```

Now storage becomes meaningful.

From there games can gradually introduce ideas such as:

```text
files
folders
capacity
location
save
load
copy
move
```

The concept exists because the player needed it.

---

# Phase 9: Files and Folders

Only after the player has a reason to store things should files and folders appear.

A file becomes:

> **A thing I saved.**

A folder becomes:

> **A way of organising the things I saved.**

From there the game can introduce:

```text
name
type
size
path
copy
move
rename
delete
restore
```

The player should manipulate these concepts visually before requiring exact technical terminology.

---

# Phase 10: Networking

Networking should begin with a problem.

```text
I HAVE SOMETHING HERE.

I NEED IT OVER THERE.
```

Now we have a reason for computers to communicate.

Gameplay can gradually introduce:

```text
sender
receiver
address
route
message
packet
connection
failure
retry
```

The player need not initially know those words.

The game creates the behaviour.

The terminology explains the behaviour later.

## Success Criterion

> The player understands why networking exists before learning how networking works.

---

# Phase 11: Troubleshooting

Eventually Binary4Babies can teach one of the most valuable computing skills of all:

> **Something didn't work. Now what?**

The game should reward curiosity rather than random button pressing.

Conceptually:

```text
OBSERVE
   ↓
MAKE A GUESS
   ↓
TEST
   ↓
SEE WHAT CHANGED
   ↓
NARROW IT DOWN
   ↓
FIX IT
```

This introduces debugging and troubleshooting without initially requiring either word.

---

# Phase 12: Programming Concepts

Programming should emerge when the player wants the computer to perform a sequence repeatedly.

Start with:

```text
DO THIS
THEN THIS
THEN THIS
```

Then:

```text
DO THIS AGAIN
```

Then:

```text
IF THIS HAPPENS
DO THAT
```

The player has discovered:

```text
sequence
loops
conditions
state
logic
```

before necessarily writing conventional source code.

Textual programming can come later if appropriate.

---

# The Common Mini-Game Framework

Do not build this before the first game works.

Once several games exist, extract genuine shared requirements.

Possible shared capabilities may include:

- game launcher
- player profiles
- settings
- accessibility options
- progress
- audio controls
- input abstraction
- shared visual assets
- localisation
- optional hints
- parental controls where appropriate

The rule is:

> **Extract common infrastructure from working games. Do not invent a framework and then hope somebody writes games for it.**

---

# Player Profiles

Profiles should eventually allow progress to persist without turning Binary4Babies into a compulsory account service.

Where practical, local play should remain possible.

A profile might remember:

```text
games played
concepts encountered
player preferences
accessibility settings
achievements
optional progression
```

Do not equate completion with mastery automatically.

Playing a game once is evidence that the player played it.

Nothing more should be inferred without evidence.

---

# Progression

Binary4Babies should avoid a rigid school-like structure.

Prefer:

```text
PLAYROOM
   │
   ├── Bits
   ├── Numbers
   ├── Logic
   ├── Memory
   ├── Storage
   ├── Networks
   └── Programming
```

Players can explore.

Games may recommend other games.

For example:

```text
Enjoyed NibBall?

You might like:
ByteBounce
Count-0-1
Flip & Fit
```

Recommendations should guide rather than gate wherever possible.

---

# Difficulty

Difficulty should not mean:

> Make the explanation more confusing.

Increasing difficulty should come from using the same concept in richer situations.

For example:

```text
BEGINNER

Recognise:
1010
```

might eventually grow into:

```text
ADVANCED

Use:
1010

as part of solving something larger.
```

The concept remains intelligible.

The problem becomes richer.

---

# Accessibility

Accessibility should be treated as part of game design rather than decoration added at the end.

Where appropriate, games should avoid relying solely upon:

- colour
- sound
- fast reactions
- precise timing
- tiny text
- complex controls

A bit should not be distinguishable only because one state is red and another is green.

State should ideally have multiple cues.

For example:

```text
0

dark
empty
low

1

bright
filled
high
```

The precise implementation will vary by game.

---

# Age

Binary4Babies should not impose an artificial upper age limit.

The same game may attract:

```text
a child
a parent
a complete adult beginner
a student
an experienced engineer
```

The presentation can be playful without talking down to the player.

> **Simple does not mean childish.**

And:

> **Beginner does not mean young.**

---

# Assessment

Assessment should remain secondary to play.

Binary4Babies should not become:

```text
LESSON
 ↓
TEST
 ↓
LESSON
 ↓
TEST
```

Games themselves can provide evidence that a player understands a mechanic.

Where explicit assessment is useful, it should ideally involve applying the idea rather than recalling terminology.

Prefer:

> Use these four bits to achieve the target state.

over:

> Define the word "nibble".

The player may understand the computing concept perfectly well while temporarily forgetting its name.

Understanding matters more.

---

# Hints

Hints should preserve discovery where possible.

Rather than immediately saying:

> Set bits 1 and 3 to ON.

prefer progressively stronger hints.

```text
HINT 1
Watch what changes when you flip one segment.

HINT 2
You need two segments active.

HINT 3
Try changing the first segment.
```

The objective is to get the player moving again without stealing the discovery from them.

---

# Failure

Failure should be informative.

A player should not be punished simply because they do not yet understand the concept the game exists to teach.

Prefer:

```text
TRY
 ↓
RESULT
 ↓
UNDERSTAND SOMETHING
 ↓
TRY AGAIN
```

over:

```text
TRY
 ↓
WRONG
 ↓
GAME OVER
```

Failure can still be funny.

It can still be dramatic.

It just shouldn't make curiosity unpleasant.

---

# Retro-First Development

Binary4Babies should embrace deliberately simple technology where it suits the game.

There is no requirement for photorealism.

Early games may use:

```text
pixel art
sprites
simple animation
limited palettes
basic sound
keyboard controls
controller controls
touch
```

A four-bit game does not require a modern 3D engine merely because one exists.

Technical simplicity can reinforce the subject matter.

---

# Technology Should Follow the Game

Do not choose an engine because it appears impressive.

Choose technology appropriate to the mechanic.

Conceptually:

```text
GAME IDEA
   ↓
PLAYABLE MECHANIC
   ↓
UX REQUIREMENTS
   ↓
TECHNOLOGY
```

not:

```text
COOL ENGINE
   ↓
NOW INVENT SOMETHING TO DO WITH IT
```

Binary4Babies should remain capable of moving between platforms and rendering technologies as individual games require.

---

# Connection to BoreDOOM!

Binary4Babies may eventually provide concepts that happen to become useful within BoreDOOM!.

For example:

```text
Binary4Babies
       ↓
understand storage
       ↓
BoreDOOM! storage puzzle

Binary4Babies
       ↓
understand networks
       ↓
BoreDOOM! connection puzzle
```

But neither project should require the other.

Binary4Babies must remain a useful and enjoyable playroom independently.

BoreDOOM! must remain a useful and enjoyable game independently.

The relationship is opportunity, not dependency.

---

# Connection to FileCity

FileCity may eventually provide interesting environments or concepts for advanced Binary4Babies games.

For example:

```text
storage
files
folders
capacity
movement
```

But Binary4Babies should teach those ideas through its own simple mechanics before assuming familiarity with FileCity.

No FileCity account or installation should be required to understand storage through Binary4Babies.

---

# Connection to Charkive

Charkive's ideas may eventually inspire games involving:

```text
preservation
classification
temporary data
quarantine
recovery
```

Again, Binary4Babies should teach the underlying concept rather than becoming a Charkive tutorial.

A game about deciding what should be kept and what can be put aside may teach useful storage thinking without touching a real filesystem.

---

# Connection to VNA

Virtual Network Admin belongs much further along the journey.

Binary4Babies should not become "VNA Junior".

A player may eventually progress:

```text
Binary4Babies
      ↓
computing curiosity
      ↓
computing knowledge
      ↓
BoreDOOM! / other projects
      ↓
deeper technical learning
      ↓
professional skills
      ↓
VNA competency
```

But that pathway is optional.

A player who spends their entire time bouncing eight glowing bits around ByteBounce has used Binary4Babies correctly.

---

# Safe Learning

The early Binary4Babies games should prefer simulation over manipulation of real systems.

Teaching file deletion does not require deleting a real file.

Teaching networking does not require modifying a real network.

Teaching storage does not require access to a real disk.

The player should be free to experiment recklessly inside the game.

```text
BREAK IT
 ↓
LAUGH
 ↓
RESET
 ↓
TRY AGAIN
```

That's one of the advantages of learning through play.

---

# Quick-Win Roadmap

## Slice 1: NibBall Prototype

Build the smallest possible NibBall.

Requirements:

- four binary states
- one enjoyable interaction
- one objective
- immediate feedback
- restart

No educational overlay yet.

### Success

> Is it a game?

If not, fix that first.

---

## Slice 2: Make the Four Bits Matter

The four states must meaningfully affect gameplay.

### Success

> The player starts noticing relationships between the four states and what happens.

---

## Slice 3: The Reveal

After the player has encountered the mechanic, reveal:

```text
0
1
```

Then:

```text
4 bits = 1 nibble
```

### Success

> The technical term explains something the player has already experienced.

---

## Slice 4: Watch Real Players

Put NibBall in front of people who have not been involved in designing it.

Do not explain it first.

Observe:

```text
Do they start playing?

Do they understand the objective?

Do they experiment?

Do they notice the four states?

Does the reveal make sense afterwards?
```

### Success

> At least some of the learning happens without verbal intervention from the developer standing nearby.

If the developer has to explain the mechanic, improve the mechanic.

---

## Slice 5: BitPop

Build the simplest possible one-bit game.

Use it to test whether Binary4Babies works for concepts even smaller than NibBall.

### Success

> One bit can produce an actual enjoyable interaction.

---

## Slice 6: Common Launcher

Only after at least two games genuinely work, introduce the beginnings of the Playroom.

```text
BINARY4BABIES

[ NibBall ]
[ BitPop  ]
```

### Success

> The project feels like a growing collection rather than unrelated prototypes.

---

## Slice 7: Binary Counting

Add the first game that makes the relationship between binary state and numbers explorable.

### Success

> Players begin making predictions rather than merely reacting.

---

## Slice 8: ByteBounce

Expand from four bits to eight.

### Success

> The byte feels like an understandable extension of the nibble rather than an unrelated fact to memorise.

---

## Slice 9: Logic

Build one genuinely fun mechanic around one Boolean operation.

Do not implement every operation simultaneously.

### Success

> The player understands the operation before seeing its truth table.

---

## Slice 10: Memory

Create a mechanic in which remembering state solves a gameplay problem.

### Success

> Memory exists because the player needs it.

---

## Slice 11: Storage

Give the player something worth keeping.

Then make them decide where to keep it.

### Success

> Storage has an intuitive purpose before filesystem terminology appears.

---

## Slice 12: Networking

Give the player something here that needs to arrive over there.

### Success

> The need for communication exists before networking terminology is introduced.

---

# Medium-Term Vision

Binary4Babies becomes a recognisable playroom of computing ideas.

A player can wander from:

```text
BIT
```

to:

```text
NIBBLE
```

to:

```text
BYTE
```

to:

```text
LOGIC
```

to:

```text
MEMORY
```

to:

```text
STORAGE
```

to:

```text
NETWORKS
```

without the experience suddenly becoming a textbook.

Different games may have completely different mechanics.

The unifying principle is not their graphics.

It is their teaching philosophy.

> **Let the player experience the idea first.**

---

# Long-Term Vision

Binary4Babies can eventually form the playful entrance to a much broader computing journey.

Someone might begin knowing nothing more than:

```text
BUTTON MAKES LIGHT CHANGE
```

and gradually discover:

```text
BIT

BINARY

NIBBLE

BYTE

NUMBER

CHARACTER

LOGIC

MEMORY

FILE

STORAGE

NETWORK

PROGRAM

COMPUTER

SYSTEM
```

Not because the software marched them through a compulsory syllabus.

Because each discovery made them curious about the next one.

Some players may stop after NibBall.

Great.

Some may build their own Binary4Babies game.

Even better.

Some may eventually find themselves building software, managing machines, exploring FileCity, playing BoreDOOM!, operating infrastructure through VNA, or doing things none of these projects currently imagine.

Excellent.

Binary4Babies only needs to start the journey.

---

# Development North Star

Before implementing any educational feature, ask:

> **Can we make this something the player discovers by playing?**

Before adding terminology, ask:

> **Does the player already have an experience to attach this word to?**

Before adding instruction, ask:

> **Could the mechanic explain this instead?**

Before adding complexity, ask:

> **Is this making the idea clearer or merely making the software cleverer?**

And before shipping a game, ask the most important question:

> **Is it actually fun?**

Because Binary4Babies is not homework with sound effects.

It is a game collection.

The learning is hiding inside.

## North Star

> **Play first. Understand second. Name it third. Stay curious.**
