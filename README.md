# UI Toolkit Assignment Submission for Oversight by Ori Ben Nun 

I will manage this readme file as a dev diary while working on this assignment, and then I will add more details and information after I finish, before submitting.

## Dev Diary:

### 7/5/2026:

2230: Just created a new Unity project and this readme file.
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

2315: Sent the plan prompt to Claude Code (Claude Sonnet 4.6 medium effort):

"

Here's the planned architecture for the program:
1. Data model (Source of truth. stores the nodes as tree structure)
2. Tree indexing (sits on top of the model. using a Dictionary<NodeId, TreeNode> for O(1) lookup. also responsible
   for flattening the tree into the visible row list)
3. Interaction logic (Expand/collapse, selection, visibility toggles)
4. Drag/drop + validation (Validation check. if invalid, don't allow the operation. if valid, update the model, which
   will be followed by tree re-indexing and UI rebuild)
5. UI Toolkit view (using ListView for flattened virtualized list of items, rebuilds on every change)

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

0140:
Got familiar with the UI Builder in Unity, and made a few manual changes in the USS and UXML files to understand the flow of working with UI Toolkit.
Tomorrow I will start reading the generated code and see that everything is according to the plan, architecture and constraints.

### 8/5/2026:

1700:
Resumed working on the project. Started reading the generated code.