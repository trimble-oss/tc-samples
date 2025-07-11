# CDM services demo

CDMServicesDemo is a demonstration of how to use the CDM services .NET SDK:
* Organizer service .NET SDK
* Property Set service .NET SDK

The following functionality is demonstrated:
1. Create a credentials provider which is then used/re-used to create the Organizer service client and the Property Set service client.
2. Organizer
	* Trees
		* Get tree
		* List all trees in a forest
		* Create tree
	* Nodes
		* Get node
		* List all nodes in a tree
		* Create node
		* Delete node
		* Update node
		* List al node versions
	* Batch get
	* Asynchronous change list workflow
		* Create change list
		* Upload change list content
		* Get change list status
		* Get change list results
	* Using additonal properties in requests and responses
	* Using generic requests
3. Property Set
	* Libraries
		* Get library
		* Create library
	* Property set definitions
		* Get definition
		* List all definitions in a library
		* Create definition
	* Property sets (instances)
		* Get property set
		* List all property sets in a library
		* Create property set
		* Delete porperty set
		* Update property set
		* List al property set versions
	* Batch get
	* Asynchronous change list workflow
		* Create change list
		* Upload change list content
		* Get change list status
		* Get change list results
	* Using additonal properties in requests and responses
	* Using generic requests