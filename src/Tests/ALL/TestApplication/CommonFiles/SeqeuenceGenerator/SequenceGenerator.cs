<?xml version="1.0" encoding="utf-8"?>
<Application>
  <NavigationItems>
    <Items>
      <Item Id="LookupCascade" IsNewNode="True">
        <Items IsNewNode="True">
          <Item Id="@00d8b0fa-2d34-4249-b2db-bc1a37ffc381" ViewId="LookupCascade_Order_ListView" IsNewNode="True" />
        </Items>
      </Item>
    </Items>
  </NavigationItems>
  <Options CollectionsEditMode="Edit" />
  <ReactiveModules>
    <LookupCascade>
      <ClientDatasource>
        <LookupViews>
          <ClientDatasourceLookupView Id="Accesories" LookupListView="LookupCascade_Accessory_LookupListView" IsNewNode="True" />
          <ClientDatasourceLookupView Id="Products" LookupListView="Product_LookupListView" IsNewNode="True" />
        </LookupViews>
      </ClientDatasource>
    </LookupCascade>
  </ReactiveModules>
  <Views>
    <ListView Id="LookupCascade_Accessory_LookupListView">
      <Columns>
        <ColumnInfo Id="Product" PropertyName="Product.Oid" Index="2" Caption="Product" ClientVisible="False" IsNewNode="True" Removed="True" />
      </Columns>
    </ListView>
    <DetailView Id="LookupCascade_Order_DetailView">
      <Items>
        <PropertyEditor Id="Accessory" PropertyEditorType="Xpand.XAF.Modules.LookupCascade.ASPxLookupCascadePropertyEditor" View="LookupCascade_Accessory_LookupListView" />
        <PropertyEditor Id="AggregatedOrders" View="LookupCascade_Order_ListView" />
        <PropertyEditor Id="Product" PropertyEditorType="Xpand.XAF.Modules.LookupCascade.ASPxLookupCascadePropertyEditor">
          <LookupCascade CascadeMemberViewItem="Accessory" CascadeColumnFilter="Product" />
        </PropertyEditor>
      </Items>
    </DetailView>
    <ListView Id="LookupCascade_Order_ListView" AllowEdit="True" NewItemRowPosition="Top" DetailViewID="LookupCascade_Order_DetailView">
      <Columns>
        <ColumnInfo Id="Product" PropertyEditorType="Xpand.XAF.Modules.LookupCascade.ASPxLookupCascadePropertyEditor" SortIndex="0" SortOrder="Ascending">
          <LookupCascade Synchronize="True" CascadeMemberViewItem="Accessory" CascadeColumnFilter="Product" />
        </ColumnInfo>
        <ColumnInfo Id="Accessory" PropertyEditorType="Xpand.XAF.Modules.LookupCascade.ASPxLookupCascadePropertyEditor" View="LookupCascade_Accessory_LookupListView">
          <LookupCascade SynchronizeMemberViewItem="Product" SynchronizeMemberLookupColumn="ProductName" />
        </ColumnInfo>
      </Columns>
    </ListView>
  </Views>
</Application>