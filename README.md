# UI Toolkit Assignment Submission for Oversight by Ori Ben Nun 

I will manage this readme file as a dev diary while working on this assignment, and then I will add more details and information after I finish, before submitting.

## Dev Diary:

### 7/5/2026:

#### 2230:

Just created a new Unity project and this readme file.
So far I've read the assignment few times, and used Claude Chat to help me understand how the UI Toolkit works and what are the best practices for using it.
The most important thing I've learned that is very relevant to this assignment is that the UI Toolkit has a built in ListView component which is a flat virtualized list of items, meaning it's exactly what we want for our case of 2500+ items (renders only visible and reuses items).
Here's the chat with Claude: https://claude.ai/share/281764f1-82cb-402b-b7eb-4c1276affd7c

Added the assignment to the directory and ran Claude/Init to start building the agent knowledge base (CLAUDE.md), using **Claude Sonnet 4.6 medium effort.**

After carfully reading the assignment again, I decided on the following 5-layer architecture (which is basically what already mentioned in the assignment PDF):

1. Data model (Source of truth. stores the nodes as tree structure)
2. Tree indexing (sits on top of the model. using a Dictionary<NodeId, TreeNode> for O(1) lookup. also responsible for flattening the tree into the visible row list)
3. Interaction logic (Expand/collapse, selection, visibility toggles)
4. Drag/drop + validation (Validation check. if invalid, don't allow the operation. if valid, update the model, which will be followed by tree re-indexing and UI rebuild)
5. UI Toolkit view (using ListView for flattened virtualized list of items, rebuilds on every change)

From my understanding, the main trade-off with this approach is the re-indexing of the tree upon every re-order (succesful drag and drop) which can become expensive with a very large number of nodes (probably larger than 2500 though), and the UI rebuilds which will also needs to be considered. However, this is not a big deal since the ListView is virtualized and only renders the visible items (so adding items won't affect that part).
Another major pitfall is search/filter indexing rebuild on every keystroke (very expensive), but this can be mitigated by debouncing the search input.

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
5. UI Toolkit view (The only MonoBehaviour in the architecture.using ListView for flattened virtualized list of items, rebuilds on every change)

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

Got familiar with the UI Builder in Unity, and made a few manual changes in the USS and UXML files to understand the flow of working with UI Toolkit.
Tomorrow I will start reading the generated code and see that everything is according to the plan, architecture and constraints.

### 8/5/2026:

#### 1700:

Resumed working on the project. Started reading the generated code from top to bottom (model to UI).
So far it seems good: we have a single MonoBehaviour (TreeViewController) to control the UI during runtime and coordinate user input events, while the rest of the code is plain C# and UXML/USS.

Another tradeoff I just realized (and confirmed with Claude) is that we are basically guaranteed to have cache misses every time we traverse the tree, since each TreeNode is a seperate heap allocation.
In this case, the assignment mandates that we use a tree structure, and also 2500 nodes is not a big deal.
But if we had 250k nodes, this wouldn't cut it, and we'd better use multiple arrays of nodes to represent the tree structure while benefiting from cache locality.

#### 1800:

Made a few minor manual changes as I went through the code.
Seems like seperation of data mutation isn't perfect (currently the drag drop validator mutates the tree structure directly). Fixing it with Claude. Here's the prompt:
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

Before moving on to reading the indexing layer, I want to create the data sample during editor-time instead of runtime (to improve startup loading times and have a stable dataset for testing).
Also, adding missing layer types according to the assignment.
Here's the plan prompt for Claude:
"
I want to create the data sample during editor-time instead of runtime.
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
we can safely generate it during editor time.
"

pushed the editor time generation refactor.
pushed small dict null check bugfix in GetNodeById, detected during runtime test.
changed Resource folder fetch to [SerializedField] on TreeViewController for better runtime performance and static access.

#### 0215:

Ran the tests (written by Claude upon NUnit) using the Test Runner, and they all passed :)
did some manual + Claude assisted changes in the USS for better UI appearance (bigger rows, spacing, bigger fonts, bg color, etc.)

#### 0300:

Fixing an unintended behavior (Claude misunderstood the instructions) by explaining what the "visibility state" actually should mean (I think lol):
"
the visibility button shouldn't affect the actual item hidden/shown rendering state, it's just to simulate if the
"layer" (or group/subgroup of layers) are currently "visible on the surface", which is just a flag state in our
case. so change the visibility button to a tri-state checkbox
"

---- Went on a break ----

#### 0440:

While writing about the single MonoBehaviour in the tradeoffs part, and I went back to the instructions and re-read them, I realized it was written there to avoid using a single MonoBehaviour for "everything". So here's my prompt for Claude to fix it:
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

After re-thinking it a lot during the day break, I decided to simplify the original pure C# approach to istead mainly use the MB managers.
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

Responded again, but I forgot to copy it and unfortunately I lost the message. It was about making sure we avoid double dependencies, and we don't confuse responsibilities, for example: currently the drag validator also mutates the data, while it should return a boolean answer to the interaction managers, which asks the data manager to mutate the data.

Accepting the proposed plan, which basically keeps the old architecture structure of 5 layers, but changes the data flow to be event-based and hirerchical instead of direct calls and double dependencies/references.
Also removing the tests, which were a nice feature Claude added, but it wasn't asked in the instructions, and it's holding me back from moving faster.

#### 1715:

Starting to go over all the generated code from top to bottom again. Starting with the model scripts.

Model class (TreeNode) looks good. Simplified it a bit (changed to public ctr and other minor stuff)

#### 1730:
updated claude.md and splitted runtime classes between the folders (according to layers).

Moving on to the index layer. starting by asking Claude to combine the pure C# class into the manager, as it doesn't actually benefit us, so combining will improve readability and still keep the file short enough.

Most Important Tradeoffs:
1. Using Dict for fast lookup O(1), but being forced to use heap allocations for each node (breaks cache locality). Fine for 2500 nodes, but not for 250k nodes.
2. Using List<TreeNode> for _children, which is not ideal for cache locality (although uses array under the hood, but we don't ensure locality during allocation), but is fine for 2500 nodes.
3. Using MonoBehaviours instead of pure C# classes for better readability and simplicity. Using pure C# classes could squeeze more performance, but under the tight time constraints, the complexity of the code is more important than the tiny bit of performance.
4. Using event-based data flow instead of direct calls and reference holding. Reference holding is faster in terms of performance, but it creates double dependencies and confuses responsibilities, which according to the instructions is a more important focus of this assignment.