# UI Toolkit Assignment Submission for Oversight by Ori Ben Nun

### How to open and run this project:

1. clone this repository
2. open the project through the Unity Hub with the correct Unity Editor version (6000.4.5f1)
3. run the project

There is also a build of the project here: https://drive.google.com/file/d/1qR6QmCkdl7UdUGfDGuxQMqUlj1TtPaMT/view?usp=sharing

To run the build:
1. Extract the zip file
2. Run the Oversight_UI_Toolkit_Test.exe file

Video of the build: https://drive.google.com/file/d/1_8b1j0XhOREfxN_ZAiCTmNdegg7wuggr/view?usp=sharing

To control the UI, you can:

1. Use the mouse to select, and drag/drop nodes (while dragging a node, hovering between nodes will show a thin blue
   line
   between them, indicating where the node will land, and a blue border + background will appear around a group node if
   you are about to move it into it)
2. Use the keyboard to navigate and expand/collapse nodes (arrow keys and space/enter)
3. Use the search bar to filter nodes by name (case-insensitive, partial match, finds both groups and layers)
4. Use the expand/collapse arrow on the left of the group's name to expand/collapse the group
5. Use the visibility toggle on the rightside of the node's layer to "hide"/"show" the layer (mutates the underlying
   data, but the toggle is the only visual indicator of the visibility state with a **tri-state** checkbox
   visualization)

### Unity version used: 6000.4.5f1

### Architecture Overview:

I'll start by saying that the architecture and prinicples I went by have changed quite significantly since I first
started working on this assignment, and I felt how I better understand the task at hand and the tradeoffs with every
iteration (documetned in the dev diary).

**So here's the final architecture, written with Claude Sonnet 4.6 medium effort (for better clarity and accuracy):**

4 runtime handlers + 1 pure validator, explicit dependency chain, no mediator.

Layers (top → bottom)

1. RenderingHandler ← UI only. ListView lifecycle, drag/drop pointer events, visual feedback
2. DragDropValidator ← Pure C# constraint layer. Drop legality, descendant checks
3. InteractionsHandler ← Intent only. Selection, expand/collapse, search, keyboard nav, drop execution
4. IndexHandler ← Derived state. Flat list, id map, filter, reveal
5. DataHandler ← Source of truth. Tree structure, mutation, persistence

Each handler knows only the layers **below** it. No upward references.

### Main technical decisions and tradeoffs:

1. My first, and most important decision, was to start with an end-to-end implementation by Claude Code (with my
   instructions and high-level architecture), and iterate over it while I'm getting into the trenches and imrove my
   understanding of both UI Toolkit (which is a first time for me),
   and the assignment's specific requirements, pitfalls, and the mindset behind it. I find this approach very helpful in
   cases like this, where this is not neccesarily my comfort zone. It helps me quickly understand the new domain(s) and
   adjust as I go forward (and sometimes back, but that's part of the learning process). I'd say it was a good call
   overall. I feel like I saved a lot of time by not starting from scratch (although it seemed like going back and forth
   for some time), while still finishing with a codebase I designed and know well.
2. Using Dictionary<NodeId, TreeNode> for fast lookup of O(1). However, in practice we're being forced to use heap
   allocations for each node (breaks cache locality). Fine for 2500 nodes, but not for 250k nodes.
2. Using List<TreeNode> for the tree, which is not ideal for cache locality (although uses array under the hood, but we
   don't ensure locality during allocation). Like mentioned above, this is fine for 2k nodes but probably not for 250k
   nodes. This will most likely demand a different approach for the runtime data structure for 100K+ nodes (probably
   plain array based).
3. Using MonoBehaviours instead of pure C# classes for better readability and simplicity. Using pure C# classes could
   squeeze more performance if done right, but under the tight time constraints - the complexity of the code is more
   important than the
   tiny bit of performance. Improtrant to note that I actually did start with only pure C# classes and a single
   MonoBehaviour, but I later decided that it's just pulls me back for no significant gain in this assignment context).
4. Using event-based data flow instead of direct calls and reference holding (again, I started the other way and
   switched). Reference holding would usually be faster and cheaper in
   terms of performance, but it pulls toward double dependencies, redundant references, and confuses responsibilities,
   which seems that according to
   the instructions is a more important focus for this specific assignment.
5. Anyone who needs data during runtime asks the DataHandler for it instead of holding a reference to it which was
   passed during initialization. It's a tradeoff because we will add a small overhead of a method call and a bit less
   optimized CPU-RAM usage, however this way we ensure true single source of truth (SSOT). Data and updates always flow
   down and requests flow up.
6. GC pressure on every rebuild — each expand/collapse, filter, or drop allocates a brand new flat list and discards the
   old one. Basically this triggers GC on every interaction with the list. Not noticable with 2K nodes, and fixable by
   clearing and refilling the same list in-place, but not done here for simplicity sake.

### How virtualization/indexing works

Virtualization is handled by the ListView component itself — it only renders visible rows and reuses off-screen
elements. Our flat list is the data source it pulls from (keeping the tree structure transparent to the ListView).

The flat list is rebuilt on every expand/collapse, visibility toggle, filter change, or drag-drop. Without a filter,
it's a single DFS pass that skips children of collapsed nodes. With a filter, it uses a two-pass approach: pass 1
collects all name-matching nodes + their ancestors into an unordered set; pass 2 does a DFS and outputs only nodes
in that set, preserving correct tree order.

For indexing, a dictionary is built once at startup via a recursive DFS over the root nodes. This gives O(1) node
lookup by id. The dictionary is never rebuilt on data mutation — a reorder or move only affects the flat list, not
the id map.

### How search/filtering works

Search input is debounced, meaning only 300ms after the last keystroke the query is applied. This avoids rebuilding the
flat
list on every keypress, which would be expensive even at 2K nodes.

Filtering is non-destructive, it never touches the tree structure or the id map. Only the flat list is rebuilt which
triggers the renderer rebuild cycle.

When the search clears, the previously selected node is revealed and scrolled into view (implementing the RevealNode
method), so the user doesn't lose their place and the node is still selected.

### How drag/drop updates the model and rejects invalid moves

Drag/drop is achieved with UI Toolkit pointer-events callbacks based. We listen to pointer down event on each node (
using userData with
pointerId inside a wrapper class), and then pointer move and pointer up events on the ListView element.

On pointer down, the dragged node is captured. On pointer move, the hovered row and drop position are computed from the
pointer's Y coordinate and the scroll offset.

Drop position has three modes: "before", "after", or "into" a group. For group rows, the top and bottom 20% of the row
trigger before/after; the middle zone triggers into. For item rows, it's a straight top/bottom split from the middle of
the row.

Validation runs on every move event — if the drop would be invalid, the visual indicator is cleared immediately.
Rejected cases: dropping onto self, dropping into own subtree, dropping an item at root level (only groups can live at
root), dropping into a non-group node. It's basically just a single boolean method with a few helpers.

On pointer up, the drop is committed: the node is removed from its old parent, inserted at the computed position in
the new parent. The tree structure mutation itself always happens on the model layer after a request by the interaction
layer (after approval of the validation layer), and the data mutation event propagates through the index and rendering
layers.

Finally, the moved node is then revealed and scrolled into view (not an actual need, because this is basically
achieved by design because the target is in view, but I wanted to use the RevealNode method at least twice).

### Known limitations

1. As mentioned above, my data structures selection is very suitable for 2500 nodes, and will probably work great with
   up to 10K or even 50K nodes. But after that, the performance will probably start to drop and a different solution
   will
   be needed.
2. The data sample is static and loaded and copied into memory once during startup, which means all runtime changes will
   be lost when the application is closed. This is by design, but it's still a limitation nevertheless.
3. No undo/redo — any drag/drop or visibility change is immediately committed to the runtime model. Everything can be
   reverted manually, but it's a bit annoying, especially if the original group is forgotten too.
3. I couldn't think of more limitations worth mentioning, and would love to hear from you if you find or can think of
   any!

### Approximate time spent

I believe my net work time is **about 10-11 hours.**
I know that it's about 30% above the suggested time, but I believe the main reason was that, unfortunately, I wasn't
able to do it in one sitting, but instead it was spread over a few days, sometimes giving me less than an hour of work
time in a row. This is not an excuse, but I hope it will explain a bit why the time taken is a bit more than expected.
However, I do believe the extra time wasn't taken to overbuild the assignment, but rather to ensure that I met the
requirements of the assignment (while keeping it up to my standards).

### AI usage disclosure:

I've used Claude Code Sonnet 4.6 (medium effort) to help me with almost every step of the process (except for this
README file, and even here it helped a couple of times haha).
I would say most of the codebase was originally written by Claude, but most if not all of it was changed during
iterative process of me reading the code and suggested changes and instruct Claude how to proceed.
I believe I explained in a lot of detail what my iterative approach was with this assignment earlier, however, to make
my process as transparent as possible, and to give a better idea of how AI was used (as I cannot truly answer questions
like "Which parts were AI-assisted" or mark "Any AI-generated code that remains in the submission"), I've tried to
document as much of the process as possible below in the Dev Diary section below. I hope it will complete the picture
about how I use AI with Unity.

## Dev Diary:

### 7/5/2026:

#### 2230:

Just created a new Unity project and this readme file.
So far I've read the assignment few times, and used Claude Chat to help me understand how the UI Toolkit works and what
are the best practices for using it.
The most important thing I've learned that is very relevant to this assignment is that the UI Toolkit has a built in
ListView component which is a flat virtualized list of items, meaning it's exactly what we want for our case of 2500+
items (renders only visible and reuses items).
Here's the chat with Claude: https://claude.ai/share/281764f1-82cb-402b-b7eb-4c1276affd7c

Added the assignment to the directory and ran Claude/Init to start building the agent knowledge base (CLAUDE.md), using
**Claude Sonnet 4.6 medium effort.**

After carfully reading the assignment again, I decided on the following 5-layer architecture (which is basically what
already mentioned in the assignment PDF):

1. Data model (Source of truth. stores the nodes as tree structure)
2. Tree indexing (sits on top of the model. using a Dictionary<NodeId, TreeNode> for O(1) lookup. also responsible for
   flattening the tree into the visible row list)
3. Interaction logic (Expand/collapse, selection, visibility toggles)
4. Drag/drop + validation (Validation check. if invalid, don't allow the operation. if valid, update the model, which
   will be followed by tree re-indexing and UI rebuild)
5. UI Toolkit view (using ListView for flattened virtualized list of items, rebuilds on every change)

From my understanding, the main trade-off with this approach is the re-indexing of the tree upon every re-order (
succesful drag and drop) which can become expensive with a very large number of nodes (probably larger than 2500
though), and the UI rebuilds which will also needs to be considered. However, this is not a big deal since the ListView
is virtualized and only renders the visible items (so adding items won't affect that part).
Another major pitfall is search/filter indexing rebuild on every keystroke (very expensive), but this can be mitigated
by debouncing the search input.

#### 2315:

Sent the plan prompt to Claude Code (Claude Sonnet 4.6 medium effort):

"

Here's the planned architecture for the program:

1. Data model (Source of truth. stores the nodes as tree structure)
2. Tree indexing (sits on top of the model. using a Dictionary<NodeId, TreeNode> for O(1) lookup. also responsible
   for flattening the tree into the visible row list)
3. Interaction logic (Expand/collapse, selection, visibility toggles)
4. Drag/drop + validation (Validation check. if invalid, don't allow the operation. if valid, update the model, which
   will be followed by tree re-indexing and UI rebuild)
5. UI Toolkit view (The only MonoBehaviour in the architecture.using ListView for flattened virtualized list of items,
   rebuilds on every change)

Here what you should keep in mind:
From my understanding, the main trade-off with this approach is the re-indexing of the tree upon every re-order
(succesful drag and drop) which can become expensive with a very large number of nodes (probably larger than 2500
though), and the UI rebuilds which will also needs to be considered. However, this is not a big deal since the
ListView is virtualized and only renders the visible items (so adding items won't affect that part).
Another major pitfall is search/filter indexing rebuild on every keystroke (very expensive), but this can be
mitigated by debouncing the search input.

"

Claude's approved plan is at "approved_initial_implementation_plan.md" at root directory.

-----Went on a break-----

#### 0140:

Got familiar with the UI Builder in Unity, and made a few manual changes in the USS and UXML files to understand the
flow of working with UI Toolkit.
Tomorrow I will start reading the generated code and see that everything is according to the plan, architecture and
constraints.

### 8/5/2026:

#### 1700:

Resumed working on the project. Started reading the generated code from top to bottom (model to UI).
So far it seems good: we have a single MonoBehaviour (TreeViewController) to control the UI during runtime and
coordinate user input events, while the rest of the code is plain C# and UXML/USS.

Another tradeoff I just realized (and confirmed with Claude) is that we are basically guaranteed to have cache misses
every time we traverse the tree, since each TreeNode is a seperate heap allocation.
In this case, the assignment mandates that we use a tree structure, and also 2500 nodes is not a big deal.
But if we had 250k nodes, this wouldn't cut it, and we'd better use multiple arrays of nodes to represent the tree
structure while benefiting from cache locality.

#### 1800:

Made a few minor manual changes as I went through the code.
Seems like seperation of data mutation isn't perfect (currently the drag drop validator mutates the tree structure
directly). Fixing it with Claude. Here's the prompt:
"
Seems like seperation of data mutation isn't perfect (currently the drag drop validator mutates the tree structure
directly). I want the TreeNode fields to be protected and can be mutated safely only from within.
"

#### 1830:

Ran the project for the first time (Play Mode) and saw it's working (about 80% of the way there).
Pushed minor changes and bugfix due to runtime errors.

---- Had to leave for a while ----

#### 0100:

Updated Claude.md with the current state of the project.

Before moving on to reading the indexing layer, I want to create the data sample during design-time instead of runtime (
to improve startup loading times and have a stable dataset for testing).
Also, adding missing layer types according to the assignment.
Here's the plan prompt for Claude:
"
I want to create the data sample during design-time instead of runtime.
Also, adding missing layer types according to the assignment, here's the quote from the instructions:
The UI represents a mission planning layer panel. A user can manage:
• Map layers
• 3D model layers
• Camera layers
• Sensor layers
• Groups and sub-groups
so we need to add the layerType as new enum and pick random layer while generating the data items (add a new property
to the TreeNode for this new layerType and use it when naming the node).

Generate the sample data (~2500 nodes, 6 hierarchy levels, mixed nodeTypes) in a smart way that looks similar to a
real case scenario for "mission-critical systems used by real customers in the field"
"

After reading the plan, I added this clarification:
"
Just to make clear - the data is static and never mutates (runtime loads the
data sample and mutates the live runtime data model). the generated data can be
in any convenient and readable format which is still runtime-load performence
awered
"

Added another clarification after the 2nd iteration:
"
The GUIDs should never change, not for the static nor the live data model, so
we can safely generate it during design time.
"

pushed the design time generation refactor.
pushed small dict null check bugfix in GetNodeById, detected during runtime test.
changed Resource folder fetch to [SerializedField] on TreeViewController for better runtime performance and static
access.

#### 0215:

Ran the tests (written by Claude upon NUnit) using the Test Runner, and they all passed :)
did some manual + Claude assisted changes in the USS for better UI appearance (bigger rows, spacing, bigger fonts, bg
color, etc.)

#### 0300:

Fixing an unintended behavior (Claude misunderstood the instructions) by explaining what the "visibility state" actually
should mean (I think lol):
"
the visibility button shouldn't affect the actual item hidden/shown rendering state, it's just to simulate if the
"layer" (or group/subgroup of layers) are currently "visible on the surface", which is just a flag state in our
case. so change the visibility button to a tri-state checkbox
"

---- Went on a break ----

#### 0440:

While writing about the single MonoBehaviour in the tradeoffs part, and I went back to the instructions and re-read
them, I realized it was written there to avoid using a single MonoBehaviour for "everything". So here's my prompt for
Claude to fix it:
"
the instructions clearly state: "Avoid putting everything in one MonoBehaviour."
To make sure we follow this guideline, I want you to seperate the TreeViewController into multiple MonoBehaviours,
each (ideally) communicating with a single layer - keeping the SOC between the 5 layers (and improving it, as
currently TreeViewController touches all 5 layers). Make a plan to improve SOC, use multiple MB managers to handle
runtime with single responsabilities, and follow the assignment instructions as they are.
So here's the IDEA of the MonoBehaviour managers we need for now:

1. DataHandler: loads, keeps and mutate the data by request by other managers. single source of truth. no one else
   keeps a reference to it, always asks.
2. IndexHandler: handles indexing and visible rows. keep the lookup dict which populates once from the DataHandler
   data.
3. InteractionsHandler: all user interactions. fetches data and asks to mutate data from the handler
4. RenderingHandler: handles the UI rebuilding"

#### 0600:

Pushed the managers refactor.

---- Going to bed ----

### 10/5/2026

#### 1630:

After re-thinking it a lot during the day break, I decided to simplify the original pure C# approach to istead mainly
use the MB managers.
The reason is to prioritize readability and code organization over performance and scalability.

Simplify prompt:
"
I decided to simplify the original pure C# approach to istead mainly use the MB managers. No need for asmdefs. let's
keep it simple and clever instead of over-engineered.
"

Responded to proposed plan:
"
Nope. first - drop the tests for now. second - I want the program to be handled
by 5 managers in a clear hirerchy, cleanly seperated, each dedicated to handle
only a signle layer of the architecture. if a layer needs require pure C#
classes, it's ok, but I think only the data model and validator are the only
ones who actually need it
"

Responded again, but I forgot to copy it and unfortunately I lost the message. It was about making sure we avoid double
dependencies, and we don't confuse responsibilities, for example: currently the drag validator also mutates the data,
while it should return a boolean answer to the interaction managers, which asks the data manager to mutate the data.

Accepting the proposed plan, which basically keeps the old architecture structure of 5 layers, but changes the data flow
to be event-based and hirerchical instead of direct calls and double dependencies/references.
Also removing the tests, which were a nice feature Claude added, but it wasn't asked in the instructions, and it's
holding me back from moving faster.

#### 1715:

Starting to go over all the generated code from top to bottom again. Starting with the model scripts.

Model class (TreeNode) looks good. Simplified it a bit (changed to public ctr and other minor stuff).

#### 1730:

updated claude.md and splitted runtime classes between the folders (according to layers).

Moving on to the index layer. starting by asking Claude to combine the pure C# class into the manager, as it doesn't
actually benefit us, so combining will improve readability and still keep the file short enough.

Moving back to DataHandler (missed it).
Saw there are several List<TreeNode> there, so sent Claude:
"
I want a signle _roots list which is the mutable one. no need to keep a copy of the original roots list, it doesn't
matter during runtime. anyone who needs to know about roots during runtime should ask the DataHandler for them, so we
have a true SSOT
"

#### 1800:

Looking good. Now DataHandler acts as the SSOT of the tree data during runtime. The tree data is represented by a
List<TreeNode> of the root nodes (the top groups), each holding a List<TreeNode> of its children. which means this is a
classic DFS tree structure.
The class is responsible for loading the static data (generated during design time), keeping and mutating the data by
request by other managers. no one else keeps a reference to it, always asks.

#### 1820:

Now moving back to the index layer, single MB class now: IndexHandler.
The main idea: keep a dictionary of nodes by id for fast lookup, and a list of root nodes (on DataHandler) for fast DFS
traversal (for search and filter).

Sent Claude:
"
currently RevealNode is both isn't in use by nobody AND isn't completed according to the assignment instructions.
Here's the quote about it: "RevealNode should expand parents and scroll to the item."
So let's add a new event by the end of the method which calls the renderingHandler to scroll to the correct node.
We should trigger RevealNode in two cases:

1. after an item is dragged (so it will be in focus)
2. after item was selected during a search, and then the user deleted the search term (so the found item will still
   be in focus)
   "

#### 1900:

Saw another issue, sent Claude:
"
why do we need to rebuild the entire index dictionary when the data changes? it should'nt affect the Dict, as it's
unordered at all
"

And another issue:
"
The IndexHandler should be the owner of the flatList, RenderingHandler should handle only UI Toolkit wiring stuff.
it's just the bridge, no business logic should be there.
"

continued with:
"
but now there are many dups and redundancy between the IndexHandler and the Rendering. It's fine for the rendering to
know IndexHandler directly and not go through Interactions for everything.
"

Now IndexHandler code looks good. It owns the FlatList, the index dictionary, and the search/filter logic.

The search/filter is implemented in a two-pass approach:

1. First pass builds all included nodes in an unordered way (including ancestors).
2. Second pass outputs in correct DFS order, when we already know exactly what to keep.

The flat list is a List<(TreeNode node, int depth, VisibilityState visState)>. The reason we need it is that
the ListView UXML component is a flat list widget. It has no concept of tree hierarchy, expand/collapse, depth, or
filtering (however it is responsible for the virtualization which is a huge benefit). Our custom
flat list is the actual tree view implementation:

- Collapsed group node → its children are simply absent from the list
- Depth field → drives indent spacer width
- Filter → rebuilds the flat list only (doesn't affect the index dictionary or the tree structure) with only matching
  nodes + their ancestors

---- Taking a quick break ----

#### 2000:

small bugfix (changed string.IsNullOrEmpty to id == null). index is not the place to enforce empty ids (should accept
them)

Finished with IndexHandler, moving on to the logic layer.
Starting with a prompt for Claude:
"
moving on to the interaction logic layer. We'll begin like we did with the other layers: first make sure the
seperation between interaction and drag/drop validation is following our current design architecture of SOC and
SSOT. then, make sure there aren't any redundant fields, old logic that is overkill or isn't needed anymore, etc.
"

Me and Claude found only minor stuff to fix in regards to SOC here. most relevant is to pass a func instead of reference
to the roots list to the DragDropValidator. That's to make the live-access intent explicit and remove the fragility.

Manually changed another thing: removed the public IsValid from the interaction handler, and instead passing a reference
of the validator to the rendering handler.

Rest of the class looks good.

#### 2100:

Moved on to the validator. everything looks good, just had to fix one missing rejection rule when the target is not a
group node.

Went back to add missing and bugged use-cases for the drag/drop in the InteractionHandler.
Here's the prompt:
"
our drag/drop is missing one or two use-cases, fix it. it should allow:
• Reordering items within the same group
• Moving an item from one group to another
• Moving a group under another group
• Moving a node must preserve its children and subtree
"

Then I found and fixed two more bugs with the validator, one caused by Claude's recent change and one was an edge-case (
when trying to move a non-group item to the root of the tree, outside of any groups).

#### 2130:

Started going through the RenderingHandler.
Seems good, just added comments.

#### 2150:

Finished reading the codebase and I feel comfortable with the code and the architecture.
Moving on to bug fixes.
I'll start with the filtered list which acts very weird upon interacting with the items when filtered.

Easily fixed it with a single prompt, found 2 issues:

1. Filter mode expand toggle: AppendFiltered ignores IsExpanded — clicking expand changes the glyph but no
   children appear/disappear. Fix: hide the toggle when search is active.
2. IndexAtY misses scroll offset: localPosition.y is viewport-relative, but calculation doesn't add
   scrollView.scrollOffset.y — so drag/drop targets wrong rows when scrolled.

Now moved to the last bug I could see. here's the prompt for Claude:
"
when dragging an item and hovering above a group (except for the very upper and bottom tips), the group
should be highlighted (indicating the item will be moved there), and allow to drop it directly EVEN IF
THE GROUP IS COLLAPSED
"

Also was easily fixed by Claude. We just added a new enum to keep track of the drop mode (so we have before, into and
after), and added a corresponding USS class and Rendering simple logic

#### 2220:

Finished with the bugfixes, now re-reading the instructions to make sure everything is correct and I have'nt missed
anything.
In the meantime I built the project for the first time, and saw the data doesn't load properly. switching to development
build to see what's going on.

Saw we're missing expand/collapse all features, adding those.

Also, while I was starting to write the architecture summary, I noticed more things that can be improved.
Prompt:
"
ok, so we need to move the keyboard navigation handling from the Rendering to the Interactions. it controls the
selection, it's not a UI logic. Also move the search logic
"

#### 2315:

pushed final touches and fixes, rebuilding to ensure the build-only bug was fixed (tried to fetch the UiDocument on
Awake instead of waiting after OnEnable).
