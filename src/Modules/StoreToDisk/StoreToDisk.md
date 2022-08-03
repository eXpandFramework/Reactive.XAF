First we declare an Account BaseObject that already has a key property. 

In addition we declare a Name and a Secret properties.

Now we will register the StoreToDisk module with this line.

Next, we apply the StoreToDiskAttribute. We will use the <emphasis level="strong">name</emphasis> as lookup property, a <emphasis level="strong">machine scope</emphasis> for protection and we will serialize the <emphasis level="strong">Secret</emphasis> property.

In addition we use the DefaultClassOptionsAttribute to dispplay the object in the UI