# Batch Set Replicator


This contains a [Workspace Post-Create Event Handler](https://platform.relativity.com/9.7/Content/Building_Relativity_applications/Post_Workspace_Create_event_handlers.htm) that replicates the Batch Sets from the workspace template in [Relativity](https://www.relativity.com).


**Notes**

* Any item-level security applied in the template workspace is _not_ replicated.
* Individual batches must still be created (if autobatch is not enabled).  After creating the workspace, import documents and choose Create Batches on each individual Batch Set to begin review.
* If you want a functional RAP for direct import into Relativity, check the RAP folder.  Install the RAP to the template workspace prior to new workspace creation.