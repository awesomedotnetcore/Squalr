﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.PropertyGrid.Commands;
using System.Windows.Media;
using System.Collections;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Markup.Primitives;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Xceed.Wpf.Toolkit.PropertyGrid
{
  internal abstract class DescriptorPropertyDefinitionBase : DependencyObject
  {
    #region Members

    private string _category;
    private string _categoryValue;
    private string _description;
    private string _displayName;
    private int _displayOrder;
    private bool _expandableAttribute;
    private bool _isReadOnly;
    private IList<Type> _newItemTypes;
    private IEnumerable<CommandBinding> _commandBindings;

    #endregion

    internal abstract PropertyDescriptor PropertyDescriptor
    {
      get;
    }

    #region Initialization

    internal DescriptorPropertyDefinitionBase( bool isPropertyGridCategorized )
    {
      this.IsPropertyGridCategorized = isPropertyGridCategorized;
    }

    #endregion

    #region Virtual Methods

    protected virtual string ComputeCategory()
    {
      return null;
    }

    protected virtual string ComputeCategoryValue()
    {
      return null;
    }

    protected virtual string ComputeDescription()
    {
      return null;
    }



    protected virtual int ComputeDisplayOrder( bool isPropertyGridCategorized )
    {
      return int.MaxValue;
    }

    protected virtual bool ComputeExpandableAttribute()
    {
      return false;
    }

    protected abstract bool ComputeIsExpandable();

    protected virtual IList<Type> ComputeNewItemTypes()
    {
      return null;
    }

    protected virtual bool ComputeIsReadOnly()
    {
      return false;
    }

    protected virtual bool ComputeCanResetValue()
    {
      return false;
    }

    protected virtual object ComputeAdvancedOptionsTooltip()
    {
      return null;
    }

    protected virtual void ResetValue()
    {
    }

    protected abstract BindingBase CreateValueBinding();

    #endregion

    #region Internal Methods

    internal abstract ObjectContainerHelperBase CreateContainerHelper( IPropertyContainer parent );

    internal void RaiseContainerHelperInvalidated()
    {
      if( this.ContainerHelperInvalidated != null )
      {
        this.ContainerHelperInvalidated( this, EventArgs.Empty );
      }
    }

    internal virtual ITypeEditor CreateDefaultEditor()
    {
      return null;
    }

    internal virtual ITypeEditor CreateAttributeEditor()
    {
      return null;
    }

    internal void UpdateAdvanceOptionsForItem( MarkupObject markupObject, DependencyObject dependencyObject, DependencyPropertyDescriptor dpDescriptor, 
                                                out object tooltip )
    {
      tooltip = StringConstants.AdvancedProperties;

      bool isResource = false;
      bool isDynamicResource = false;

      var markupProperty = markupObject.Properties.FirstOrDefault( p => p.Name == PropertyName );
      if( markupProperty != null )
      {
        //TODO: need to find a better way to determine if a StaticResource has been applied to any property not just a style(maybe with StaticResourceExtension)
        isResource = typeof( Style ).IsAssignableFrom( markupProperty.PropertyType );
        isDynamicResource = typeof( DynamicResourceExtension ).IsAssignableFrom( markupProperty.PropertyType );
      }

      if( isResource || isDynamicResource )
      {
        tooltip = StringConstants.Resource;
      }
      else
      {
        if( ( dependencyObject != null ) && ( dpDescriptor != null ) )
        {
          if( BindingOperations.GetBindingExpressionBase( dependencyObject, dpDescriptor.DependencyProperty ) != null )
          {
            tooltip = StringConstants.Databinding;
          }
          else
          {
            BaseValueSource bvs =
              DependencyPropertyHelper
              .GetValueSource( dependencyObject, dpDescriptor.DependencyProperty )
              .BaseValueSource;

            switch( bvs )
            {
              case BaseValueSource.Inherited:
              case BaseValueSource.DefaultStyle:
              case BaseValueSource.ImplicitStyleReference:
                tooltip = StringConstants.Inheritance;
                break;
              case BaseValueSource.DefaultStyleTrigger:
                break;
              case BaseValueSource.Style:
                tooltip = StringConstants.StyleSetter;
                break;

              case BaseValueSource.Local:
                tooltip = StringConstants.Local;
                break;
            }
          }
        }
      }
    }

    internal void UpdateAdvanceOptions()
    {
      // Only set the Tooltip. the Icon will be added in XAML based on the Tooltip.
      this.AdvancedOptionsTooltip = this.ComputeAdvancedOptionsTooltip();
    }

    internal void UpdateIsExpandable()
    {
      this.IsExpandable = 
        this.ExpandableAttribute 
        && this.ComputeIsExpandable();
    }

    internal void UpdateValueFromSource()
    {
      BindingOperations.GetBindingExpressionBase( this, DescriptorPropertyDefinitionBase.ValueProperty ).UpdateTarget();
    }




    internal object ComputeDescriptionForItem( object item )
    {
      PropertyDescriptor pd = item as PropertyDescriptor;

      //We do not simply rely on the "Description" property of PropertyDescriptor
      //since this value is cached by PropertyDescriptor and the localized version 
      //(e.g., LocalizedDescriptionAttribute) value can dynamicaly change.
      DescriptionAttribute descriptionAtt = PropertyGridUtilities.GetAttribute<DescriptionAttribute>( pd );
      return ( descriptionAtt != null )
              ? descriptionAtt.Description
              : pd.Description;
    }

    internal object ComputeNewItemTypesForItem( object item )
    {
      PropertyDescriptor pd = item as PropertyDescriptor;
      var attribute = PropertyGridUtilities.GetAttribute<NewItemTypesAttribute>( pd );

      return ( attribute != null ) 
              ? attribute.Types 
              : null;
    }





    internal object ComputeDisplayOrderForItem( object item )
    {
      PropertyDescriptor pd = item as PropertyDescriptor;
      List<PropertyOrderAttribute> list = pd.Attributes.OfType<PropertyOrderAttribute>().ToList();

      if( list.Count > 0 )
      {
        this.ValidatePropertyOrderAttributes( list );

        if( this.IsPropertyGridCategorized )
        {
          var attribute = list.FirstOrDefault( x => ( ( x.UsageContext == UsageContextEnum.Categorized ) 
                                                    || ( x.UsageContext == UsageContextEnum.Both ) ) );
          if( attribute != null )
            return attribute.Order;
        }
        else
        {
          var attribute = list.FirstOrDefault( x => ( ( x.UsageContext == UsageContextEnum.Alphabetical ) 
                                                    || ( x.UsageContext == UsageContextEnum.Both ) ) );
          if( attribute != null )
            return attribute.Order;
        }
      }

      // Max Value. Properties with no order will be displayed last.
      return int.MaxValue;
    }

    internal object ComputeExpandableAttributeForItem( object item )
    {
      PropertyDescriptor pd = ( PropertyDescriptor )item;
      var attribute = PropertyGridUtilities.GetAttribute<ExpandableObjectAttribute>( pd );
      return ( attribute != null );
    }

    internal int ComputeDisplayOrderInternal( bool isPropertyGridCategorized )
    {
      return this.ComputeDisplayOrder( isPropertyGridCategorized );
    }

    internal object GetValueInstance( object sourceObject )
    {
      ICustomTypeDescriptor customTypeDescriptor = sourceObject as ICustomTypeDescriptor;
      if( customTypeDescriptor != null )
        sourceObject = customTypeDescriptor.GetPropertyOwner( PropertyDescriptor );

      return sourceObject;
    }

    #endregion

    #region Private Methods

    private void ExecuteResetValueCommand( object sender, ExecutedRoutedEventArgs e )
    {
      if( ComputeCanResetValue() )
        ResetValue();
    }

    private void CanExecuteResetValueCommand( object sender, CanExecuteRoutedEventArgs e )
    {
      e.CanExecute = ComputeCanResetValue();
    }

    private string ComputeDisplayName()
    {
      string displayName = PropertyDescriptor.DisplayName;
      var attribute = PropertyGridUtilities.GetAttribute<ParenthesizePropertyNameAttribute>( PropertyDescriptor );
      if( ( attribute != null ) && attribute.NeedParenthesis )
      {
        displayName = "(" + displayName + ")";
      }

      return displayName;
    }

    private void ValidatePropertyOrderAttributes( List<PropertyOrderAttribute> list )
    {
      if( list.Count > 0 )
      {
        PropertyOrderAttribute both = list.FirstOrDefault( x => x.UsageContext == UsageContextEnum.Both );
        if( ( both != null ) && ( list.Count > 1 ) )
          Debug.Assert( false, "A PropertyItem can't have more than 1 PropertyOrderAttribute when it has UsageContext : Both" );
      }
    }

    #endregion

    #region Events

    public event EventHandler ContainerHelperInvalidated;

    #endregion

    #region AdvancedOptionsIcon (DP)

    public static readonly DependencyProperty AdvancedOptionsIconProperty =
        DependencyProperty.Register( "AdvancedOptionsIcon", typeof( ImageSource ), typeof( DescriptorPropertyDefinitionBase ), new UIPropertyMetadata( null ) );

    public ImageSource AdvancedOptionsIcon
    {
      get
      {
        return ( ImageSource )GetValue( AdvancedOptionsIconProperty );
      }
      set
      {
        SetValue( AdvancedOptionsIconProperty, value );
      }
    }

    #endregion

    #region AdvancedOptionsTooltip (DP)

    public static readonly DependencyProperty AdvancedOptionsTooltipProperty =
        DependencyProperty.Register( "AdvancedOptionsTooltip", typeof( object ), typeof( DescriptorPropertyDefinitionBase ), new UIPropertyMetadata( null ) );

    public object AdvancedOptionsTooltip
    {
      get
      {
        return ( object )GetValue( AdvancedOptionsTooltipProperty );
      }
      set
      {
        SetValue( AdvancedOptionsTooltipProperty, value );
      }
    }

    #endregion //AdvancedOptionsTooltip

    #region IsExpandable (DP)

    public static readonly DependencyProperty IsExpandableProperty =
        DependencyProperty.Register( "IsExpandable", typeof( bool ), typeof( DescriptorPropertyDefinitionBase ), new UIPropertyMetadata( false ) );

    public bool IsExpandable
    {
      get
      {
        return ( bool )GetValue( IsExpandableProperty );
      }
      set
      {
        SetValue( IsExpandableProperty, value );
      }
    }

    #endregion //IsExpandable

    public string Category
    {
      get { return _category; }
      internal set { _category = value; }
    }

    public string CategoryValue
    {
      get { return _categoryValue; }
      internal set { _categoryValue = value; }
    }

    public IEnumerable<CommandBinding> CommandBindings
    {
      get { return _commandBindings; }
    }

    public string DisplayName
    {
      get { return _displayName; }
      internal set { _displayName = value; }
    }



    public string Description
    {
      get { return _description; }
      internal set { _description = value; }
    }

    public int DisplayOrder
    {
      get { return _displayOrder; }
      internal set { _displayOrder = value; }
    }

    public bool IsReadOnly
    {
      get { return _isReadOnly; }
    }

    public IList<Type> NewItemTypes
    {
      get { return _newItemTypes; }
    }

    public string PropertyName
    {
      get
      {
        // A common property which is present in all selectedObjects will always have the same name.
        return PropertyDescriptor.Name;
      }
    }

    public Type PropertyType
    {
      get
      {
        return PropertyDescriptor.PropertyType;
      }
    }

    internal bool ExpandableAttribute
    {
      get { return _expandableAttribute; }
      set 
      { 
        _expandableAttribute = value;
        this.UpdateIsExpandable();
      }
    }

    internal bool IsPropertyGridCategorized
    {
      get;
      set;
    }

    #region Value Property (DP)

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register( "Value", typeof( object ), typeof( DescriptorPropertyDefinitionBase ), new UIPropertyMetadata( null, OnValueChanged ) );
    public object Value
    {
      get
      {
        return GetValue( ValueProperty );
      }
      set
      {
        SetValue( ValueProperty, value );
      }
    }

    private static void OnValueChanged( DependencyObject o, DependencyPropertyChangedEventArgs e )
    {
      ( ( DescriptorPropertyDefinitionBase )o ).OnValueChanged( e.OldValue, e.NewValue );
    }

    internal virtual void OnValueChanged( object oldValue, object newValue )
    {
      UpdateIsExpandable();
      UpdateAdvanceOptions();

      // Reset command also affected.
      CommandManager.InvalidateRequerySuggested();
    }

    #endregion //Value Property

    public virtual void InitProperties()
    {
      // Do "IsReadOnly" and PropertyName first since the others may need that value.
      _isReadOnly = ComputeIsReadOnly();
      _category = ComputeCategory();
      _categoryValue = ComputeCategoryValue();
      _description = ComputeDescription();
      _displayName = ComputeDisplayName();
      _displayOrder = ComputeDisplayOrder( this.IsPropertyGridCategorized );
      _expandableAttribute = ComputeExpandableAttribute();
      _newItemTypes = ComputeNewItemTypes();
      _commandBindings = new CommandBinding[] { new CommandBinding( PropertyItemCommands.ResetValue, ExecuteResetValueCommand, CanExecuteResetValueCommand ) };

      UpdateIsExpandable();
      UpdateAdvanceOptions();

      BindingBase valueBinding = this.CreateValueBinding();
      BindingOperations.SetBinding( this, DescriptorPropertyDefinitionBase.ValueProperty, valueBinding );
    }
  }
}
