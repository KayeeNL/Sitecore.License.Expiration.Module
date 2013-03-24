// /*===========================================================
//    Copyright 2013 Robbert Hock
//  
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//  
// ============================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Sitecore.License.Expiration.Module
{
    /// <summary>
    /// Wrapper class for Sitecore items. Provides typed access to related items.
    /// </summary>
    public class ItemWrapper : IItemWrapper, INotifyPropertyChanging, INotifyPropertyChanged
    {
        /// <summary>
        /// Map template IDs to a corresponding type in the domain model.
        /// </summary>
        protected static readonly IDictionary<ID, Type> TypeMappings = new Dictionary<ID, Type>
            {
                {Sitecore.License.Expiration.Module.Models.Settings.TEMPLATE_ID, typeof (Sitecore.License.Expiration.Module.Models.Settings)},
            };

        /// <summary>
        /// Map contributing template IDs to a corresponding interface.
        /// If a domain model class implements the interface, it means that the template inherits this contributing template.
        /// </summary>
        protected static readonly IDictionary<ID, Type> ContributingTypeMappings = new Dictionary<ID, Type>
            {
            };

        /// <summary>
        /// Determines what templates (within scope of the domain model configuration) a certain type supports.
        /// </summary>
        /// <typeparam name="T">The type to find valid template IDs for.</typeparam>
        /// <returns>A list of template IDs that are valid for the type.</returns>
        protected static IEnumerable<ID> GetValidTemplateIdsForType<T>() where T : IItemWrapper
        {
            return TypeMappings.Concat(ContributingTypeMappings).Where(typeMapping => typeof (T).IsAssignableFrom(typeMapping.Value)).Select(typeMapping => typeMapping.Key);
        }

        /// <summary>
        /// Event that is fired before the value of a field is changed through the domain model.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Event that is fired after the value of a field is changed through the domain model.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The Sitecore item that is wrapped. It must always be passed through a constructor.
        /// </summary>
        public Item Item { get; private set; }

        /// <summary>
        /// Create a wrapper for the passed in item.
        /// </summary>
        /// <param name="item">The Sitecore item to create a wrapper for.</param>
        public ItemWrapper(Item item)
        {
            Assert.IsNotNull(item, string.Format("No item was passed to create a new '{0}' object", GetType()));
            Item = item;
        }

        /// <summary>
        /// Use this method to wrap any Sitecore item. If the item can be used in a typed way, then you will get an object of that type.
        /// </summary>
        /// <param name="item">The Sitecore item to create a typed wrapper for.</param>
        /// <returns>An item wrapper (typed, if possible).</returns>
        public static ItemWrapper CreateTypedWrapper(Item item)
        {
            return (item != null && TypeMappings.ContainsKey(item.TemplateID))
                       ? TypeMappings[item.TemplateID].InvokeMember("ctor", System.Reflection.BindingFlags.CreateInstance, null, null, new object[] {item}) as ItemWrapper
                       : new ItemWrapper(item);
        }

        /// <summary>
        /// Get a typed wrapper for the parent of the item.
        /// </summary>
        public virtual IItemWrapper Parent
        {
            get { return Item.Parent != null ? CreateTypedWrapper(Item.Parent) : null; }
        }

        /// <summary>
        /// Get a list of typed wrappers for all the child items.
        /// </summary>
        public virtual IEnumerable<IItemWrapper> Children
        {
            get
            {
                if (!Item.HasChildren)
                {
                    return null;
                }
                IEnumerable<IItemWrapper> childObjects = Item.Children.InnerChildren.Select(child => CreateTypedWrapper(child)).Where(child => child != null).OfType<IItemWrapper>();
                return childObjects.Count() > 0 ? childObjects : null;
            }
        }

        /// <summary>
        /// Get a list of typed wrappers for all the child items that can be wrapped with the specified type.
        /// </summary>
        /// <typeparam name="T">The type to filter the children with.</typeparam>
        /// <returns>The list of children of the specified type.</returns>
        public virtual IEnumerable<T> GetChildren<T>() where T : IItemWrapper
        {
            IEnumerable<IItemWrapper> childObjects = Children;
            IEnumerable<T> typedChildren = childObjects != null ? childObjects.OfType<T>() : null;
            return typedChildren != null && typedChildren.Count() > 0 ? typedChildren : null;
        }

        /// <summary>
        /// Iterates all the item's ancestors and returns the first one that is of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of ancestor that is needed.</typeparam>
        /// <returns>A typed wrapper for the first ancestor of the specified type.</returns>
        public virtual T GetFirstAncestor<T>() where T : class, IItemWrapper
        {
            IItemWrapper parent = Parent;
            if (parent == null)
            {
                return null;
            }
            else if (parent as T != null)
            {
                return parent as T;
            }
            return parent.GetFirstAncestor<T>();
        }

        /// <summary>
        /// Returns a list of all the item's descendants of the specified type.
        /// Warning: only use this if you're certain it will not return too much; could cause performance issues.
        /// </summary>
        /// <typeparam name="T">Type to filter the result with.</typeparam>
        /// <returns>The list of descendants of the specified type.</returns>
        public virtual IEnumerable<IItemWrapper> Descendants
        {
            get
            {
                Item[] descendantItems = Item.Axes.GetDescendants();
                if (descendantItems == null || descendantItems.Length == 0)
                {
                    return null;
                }
                IEnumerable<IItemWrapper> descendantObjects = descendantItems.Select(descendant => CreateTypedWrapper(descendant)).Where(descendant => descendant != null).OfType<IItemWrapper>();
                return descendantObjects.Count() > 0 ? descendantObjects : null;
            }
        }

        /// <summary>
        /// Returns a list of all the item's descendants of the specified type.
        /// Warning: only use this if you're certain it will not return too much; could cause performance issues.
        /// </summary>
        /// <typeparam name="T">Type to filter the result with.</typeparam>
        /// <returns>The list of descendants of the specified type.</returns>
        public virtual IEnumerable<T> GetDescendants<T>() where T : IItemWrapper
        {
            IEnumerable<IItemWrapper> descendants = Descendants;
            if (descendants == null || descendants.Count() == 0)
            {
                return null;
            }
            IEnumerable<T> descendantObjects = descendants.OfType<T>();
            return descendantObjects.Count() > 0 ? descendantObjects : null;
        }

        /// <summary>
        /// Gets a list of all the items that refer to this item.
        /// Warning: this only works if the link database is enabled.
        /// Visit http://sdn.sitecore.net/SDN5/Articles/Administration/Links%20Database.aspx for more information.
        /// </summary>
        public virtual IEnumerable<IItemWrapper> Referrers
        {
            get
            {
                Sitecore.Links.ItemLink[] referrers = Sitecore.Globals.LinkDatabase.GetReferrers(Item);
                if (referrers == null || referrers.Length == 0)
                {
                    return null;
                }
                IEnumerable<IItemWrapper> referrerObjects = referrers.Select(referrer => CreateTypedWrapper(referrer.GetSourceItem())).Where(referrer => referrer != null).OfType<IItemWrapper>();
                return referrerObjects.Count() > 0 ? referrerObjects : null;
            }
        }

        /// <summary>
        /// Gets a list of all the items that refer to this item and that are of the specified type.
        /// Warning: this only works if the link database is enabled.
        /// Visit http://sdn.sitecore.net/SDN5/Articles/Administration/Links%20Database.aspx for more information.
        /// </summary>
        /// <typeparam name="T">Type to filter the result with.</typeparam>
        /// <returns>A list of all the items that refer to this item and that are of the specified type.</returns>
        public virtual IEnumerable<T> GetReferrers<T>() where T : IItemWrapper
        {
            IEnumerable<IItemWrapper> referrerObjects = Referrers;
            IEnumerable<T> typedReferrerObjects = referrerObjects != null ? referrerObjects.OfType<T>() : null;
            return typedReferrerObjects != null && typedReferrerObjects.Count() > 0 ? typedReferrerObjects : null;
        }

        /// <summary>
        /// Checks to see if the wrappers wrap the same item (compares by id).
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the item ID's are the same.</returns>
        public override bool Equals(object obj)
        {
            if (obj as ItemWrapper == null)
            {
                return false;
            }
            return Object.Equals(Item.ID, ((ItemWrapper) obj).Item.ID)
                   && Object.Equals(Item.Version.Number, ((ItemWrapper) obj).Item.Version.Number)
                   && Object.Equals(Item.Language, ((ItemWrapper) obj).Item.Language);
        }

        /// <summary>
        /// Creates a hashcode (for example, for use in a hashset).
        /// </summary>
        /// <returns>A unique hashcode for the object.</returns>
        public override int GetHashCode()
        {
            return ((Item.ID.GetHashCode()*13) + 2)
                   *((Item.Version.Number.GetHashCode()*23) + 8)
                   *((Item.Language.GetHashCode()*97) + 12);
        }

        /// <summary>
        /// Can be used to signal event listeners that a property that is about to change.
        /// </summary>
        /// <param name="propertyName">The name of the property that is about to change.</param>
        protected virtual void RaisePropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Can be used to signal event listeners that a property has just changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that has just changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Get a Sitecore custom field by ID or fieldname (fallback).
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="fieldId">The Sitecore ID of the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The custom field object.</returns>
        protected virtual T GetField<T>(ID fieldId, string fieldName) where T : CustomField
        {
            return FieldTypeManager.GetField(GetField(fieldId, fieldName)) as T;
        }

        /// <summary>
        /// Get a Sitecore field by ID or fieldname (fallback).
        /// </summary>
        /// <param name="fieldId">The Sitecore ID of the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The field object.</returns>
        protected virtual Field GetField(ID fieldId, string fieldName)
        {
            return Item.Fields[fieldId] != null ? Item.Fields[fieldId] : Item.Fields[fieldName];
        }

        /// <summary>
        /// Override this method if you need a different strategy of resolving links within the domain model.
        /// </summary>
        /// <param name="link">The link field that needs to be resolved.</param>
        /// <returns>A string that represents the resolved link.</returns>
        protected virtual string ResolveLink(LinkField link)
        {
            return link.IsInternal || link.IsMediaLink
                       ? ResolveLink(link.TargetItem)
                       : link.Url;
        }

        /// <summary>
        /// Override this method if you need a different strategy of resolving links within the domain model.
        /// </summary>
        /// <param name="item">The item for which to resolve a link.</param>
        /// <returns>A string that represents the resolved link.</returns>
        protected virtual string ResolveLink(Item item)
        {
            return item != null ? Sitecore.Links.LinkManager.GetItemUrl(item) : null;
        }
    }

    /// <summary>
    /// The base class for all domain objects. Objects of this type can only be created for templates that are configured in the CompiledDomainModel configuration.
    /// </summary>
    public class DomainObjectBase : ItemWrapper
    {
        protected DomainObjectBase(Item item)
            : base(item)
        {
            Assert.IsTrue(ItemWrapper.TypeMappings.ContainsKey(item.TemplateID), string.Format("Tried to create a '{0}', but there is no domain object specified for template '{1}'", GetType().Name, item.TemplateName));
            Assert.IsTrue(GetType().IsAssignableFrom(ItemWrapper.TypeMappings[item.TemplateID]), string.Format("Tried to create a '{0}', but the template '{1}' is not valid for that type", GetType().Name, item.TemplateName));
        }
    }

    /// <summary>
    /// Interface for all wrappers and contributing templates.
    /// </summary>
    public interface IItemWrapper
    {
        Item Item { get; }
        IItemWrapper Parent { get; }
        IEnumerable<IItemWrapper> Children { get; }
        IEnumerable<T> GetChildren<T>() where T : IItemWrapper;
        T GetFirstAncestor<T>() where T : class, IItemWrapper;
        IEnumerable<IItemWrapper> Descendants { get; }
        IEnumerable<T> GetDescendants<T>() where T : IItemWrapper;
        IEnumerable<IItemWrapper> Referrers { get; }
        IEnumerable<T> GetReferrers<T>() where T : IItemWrapper;
    }

    /// <summary>
    /// Marker interface for all contributing templates (templates that are not used as domain model classes, but that are implemented by domain model classes).
    /// </summary>
    public interface IContributingTemplate : IItemWrapper
    {
    }
}

// Domain objects for set "Models"

namespace Sitecore.License.Expiration.Module.Models
{
    /// <summary>
    /// Typed wrapper class for items with template Settings.
    /// </summary>
    public class Settings : DomainObjectBase
    {
        public const string TEMPLATE_NAME = "Settings";
        public static readonly ID TEMPLATE_ID = ID.Parse("{7210C857-D27B-4325-A6FA-1346E8ECA366}");

        public Settings(Item item)
            : base(item)
        {
        }

        #region General Settings

        public const string FIELD_ALWAYS_WARN = "AlwaysWarn";

        public const string FIELD_DEFAULT_NUMBER_OF_DAYS_TO_WARN = "DefaultNumberOfDaysToWarn";

        public const string FIELD_WARNING_TITLE = "WarningTitle";

        public const string FIELD_WARNING_SUBTITLE = "WarningSubtitle";

        /// <summary>
        /// Description: If checked a Content Editor Warning is always shown on a content item
        /// </summary>
        public bool AlwaysWarn
        {
            get { return GetField<CheckboxField>(ID.Parse("{FA20D73D-64BC-4F6F-B477-4FB2C6CBC5F1}"), "AlwaysWarn").Checked; }
            set
            {
                var field = GetField<CheckboxField>(ID.Parse("{FA20D73D-64BC-4F6F-B477-4FB2C6CBC5F1}"), "AlwaysWarn");
                if (Object.Equals(field.Checked, value))
                {
                    return;
                }
                RaisePropertyChanging("AlwaysWarn");
                field.Checked = value;
                RaisePropertyChanged("AlwaysWarn");
            }
        }

        /// <summary>
        /// Description: If the Content Editor Warning should not always be shown, the warning is only shown the number of filled in days before the Sitecore license expires
        /// </summary>
        public long? DefaultNumberOfDaysToWarn
        {
            get
            {
                string strValue = GetField(ID.Parse("{49216158-E8A5-4DA3-90C0-F8C62580C923}"), "DefaultNumberOfDaysToWarn").Value;
                long result;
                return !string.IsNullOrEmpty(strValue) && long.TryParse(strValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out result) ? result : null as long?;
            }
            set
            {
                Field field = GetField(ID.Parse("{49216158-E8A5-4DA3-90C0-F8C62580C923}"), "DefaultNumberOfDaysToWarn");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("DefaultNumberOfDaysToWarn");
                field.Value = value.HasValue ? value.ToString() : null;
                RaisePropertyChanged("DefaultNumberOfDaysToWarn");
            }
        }

        public string WarningTitle
        {
            get { return GetField(ID.Parse("{62211C79-9B02-4CF3-BACC-BAA8CC237D74}"), "WarningTitle").Value; }
            set
            {
                Field field = GetField(ID.Parse("{62211C79-9B02-4CF3-BACC-BAA8CC237D74}"), "WarningTitle");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("WarningTitle");
                field.Value = value;
                RaisePropertyChanged("WarningTitle");
            }
        }

        public string WarningSubtitle
        {
            get { return GetField(ID.Parse("{65D9DD46-6F9A-4381-8CC4-B4711A6AF196}"), "WarningSubtitle").Value; }
            set
            {
                Field field = GetField(ID.Parse("{65D9DD46-6F9A-4381-8CC4-B4711A6AF196}"), "WarningSubtitle");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("WarningSubtitle");
                field.Value = value;
                RaisePropertyChanged("WarningSubtitle");
            }
        }

        #endregion

        #region Mail Settings

        public const string FIELD_DISABLE_MAIL = "DisableMail";

        public const string FIELD_MAIL_FROM = "MailFrom";

        public const string FIELD_MAIL_TO = "MailTo";

        public const string FIELD_MAIL_SUBJECT = "MailSubject";

        public const string FIELD_MAIL_CONTENT = "MailContent";

        public bool DisableMail
        {
            get { return GetField<CheckboxField>(ID.Parse("{5D9296BA-6580-409D-B8CE-B7C9B9D462FA}"), "DisableMail").Checked; }
            set
            {
                var field = GetField<CheckboxField>(ID.Parse("{5D9296BA-6580-409D-B8CE-B7C9B9D462FA}"), "DisableMail");
                if (Object.Equals(field.Checked, value))
                {
                    return;
                }
                RaisePropertyChanging("DisableMail");
                field.Checked = value;
                RaisePropertyChanged("DisableMail");
            }
        }

        /// <summary>
        /// Description: The emailaddress from where the mail notification is send that the Sitecore license file is about to expire
        /// </summary>
        public string MailFrom
        {
            get { return GetField(ID.Parse("{A4E0A6F0-D3D0-4DD4-9C87-7A74495FECD5}"), "MailFrom").Value; }
            set
            {
                Field field = GetField(ID.Parse("{A4E0A6F0-D3D0-4DD4-9C87-7A74495FECD5}"), "MailFrom");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("MailFrom");
                field.Value = value;
                RaisePropertyChanged("MailFrom");
            }
        }

        /// <summary>
        /// Description: The emailaddress to where the mail notification is send that the Sitecore license file is about to expire
        /// </summary>
        public string MailTo
        {
            get { return GetField(ID.Parse("{ACBEF3E1-BBBA-426F-B253-AB0C51BAA6AD}"), "MailTo").Value; }
            set
            {
                Field field = GetField(ID.Parse("{ACBEF3E1-BBBA-426F-B253-AB0C51BAA6AD}"), "MailTo");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("MailTo");
                field.Value = value;
                RaisePropertyChanged("MailTo");
            }
        }

        /// <summary>
        /// Description: The email subject of the mail notification that is send when the Sitecore license file is about to expire
        /// </summary>
        public string MailSubject
        {
            get { return GetField(ID.Parse("{897B87E3-EC65-48E0-A3B3-7A8E0EEB7047}"), "MailSubject").Value; }
            set
            {
                Field field = GetField(ID.Parse("{897B87E3-EC65-48E0-A3B3-7A8E0EEB7047}"), "MailSubject");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("MailSubject");
                field.Value = value;
                RaisePropertyChanged("MailSubject");
            }
        }

        /// <summary>
        /// Description: The content of the mail notification that is send when the Sitecore license file is about to expire
        /// </summary>
        public string MailContent
        {
            get { return GetField(ID.Parse("{191856C0-1BBC-441D-BC4C-ADC7BCA70D3C}"), "MailContent").Value; }
            set
            {
                Field field = GetField(ID.Parse("{191856C0-1BBC-441D-BC4C-ADC7BCA70D3C}"), "MailContent");
                if (Object.Equals(field.Value, value))
                {
                    return;
                }
                RaisePropertyChanging("MailContent");
                field.Value = value;
                RaisePropertyChanged("MailContent");
            }
        }

        #endregion
    }

    #region Contributing templates for set "Models"

    #endregion
}

#region Fixed paths from set with name "FixedPaths"

namespace Sitecore.License.Expiration.Module.FixedPaths.System.Modules.SitecoreLicenseExpirationModule
{
    /// <summary>
    /// Access the item at /sitecore/system/Modules/Sitecore License Expiration Module/Settings.
    /// The availability of the item is validated in the databases: master.
    /// </summary>
    public static class SettingsFixed
    {
        public static Sitecore.License.Expiration.Module.Models.Settings GetSettings(Database database)
        {
            return new Sitecore.License.Expiration.Module.Models.Settings(GetItem(database));
        }

        public static Sitecore.License.Expiration.Module.Models.Settings Settings
        {
            get { return GetSettings(Sitecore.Context.Database); }
        }

        public static Sitecore.License.Expiration.Module.Models.Settings SettingsFromMaster
        {
            get { return GetSettings(Database.GetDatabase("master")); }
        }

        private static Item GetItem(Database database)
        {
            Item item = database.GetItem(ID.Parse("{AAE814A9-6EB3-4F96-8F04-BD9A4B7094FB}"));
            if (item != null)
            {
                return "/sitecore/system/Modules/Sitecore License Expiration Module/Settings".Equals(item.Paths.FullPath)
                           ? item
                           : (database.GetItem("/sitecore/system/Modules/Sitecore License Expiration Module/Settings") ?? item);
            }
            else
            {
                return database.GetItem("/sitecore/system/Modules/Sitecore License Expiration Module/Settings");
            }
        }
    }
}

namespace Sitecore.License.Expiration.Module.FixedPaths.System.Modules
{
    /// <summary>
    /// Access the item at /sitecore/system/Modules/Sitecore License Expiration Module.
    /// The availability of the item is validated in the databases: master.
    /// </summary>
    public static class SitecoreLicenseExpirationModuleFixed
    {
        public static ItemWrapper GetItemWrapper(Database database)
        {
            return new ItemWrapper(GetItem(database));
        }

        public static ItemWrapper ItemWrapper
        {
            get { return GetItemWrapper(Sitecore.Context.Database); }
        }

        public static ItemWrapper ItemWrapperFromMaster
        {
            get { return GetItemWrapper(Database.GetDatabase("master")); }
        }

        private static Item GetItem(Database database)
        {
            Item item = database.GetItem(ID.Parse("{9DD99268-544A-4B55-9915-2C1677983D97}"));
            if (item != null)
            {
                return "/sitecore/system/Modules/Sitecore License Expiration Module".Equals(item.Paths.FullPath)
                           ? item
                           : (database.GetItem("/sitecore/system/Modules/Sitecore License Expiration Module") ?? item);
            }
            else
            {
                return database.GetItem("/sitecore/system/Modules/Sitecore License Expiration Module");
            }
        }
    }
}

namespace Sitecore.License.Expiration.Module.FixedPaths.System
{
    /// <summary>
    /// Access the item at /sitecore/system/Modules.
    /// The availability of the item is validated in the databases: master.
    /// </summary>
    public static class ModulesFixed
    {
        public static ItemWrapper GetItemWrapper(Database database)
        {
            return new ItemWrapper(GetItem(database));
        }

        public static ItemWrapper ItemWrapper
        {
            get { return GetItemWrapper(Sitecore.Context.Database); }
        }

        public static ItemWrapper ItemWrapperFromMaster
        {
            get { return GetItemWrapper(Database.GetDatabase("master")); }
        }

        private static Item GetItem(Database database)
        {
            Item item = database.GetItem(ID.Parse("{08477468-D438-43D4-9D6A-6D84A611971C}"));
            if (item != null)
            {
                return "/sitecore/system/Modules".Equals(item.Paths.FullPath)
                           ? item
                           : (database.GetItem("/sitecore/system/Modules") ?? item);
            }
            else
            {
                return database.GetItem("/sitecore/system/Modules");
            }
        }
    }
}

namespace Sitecore.License.Expiration.Module.FixedPaths
{
    /// <summary>
    /// Access the item at /sitecore/system.
    /// The availability of the item is validated in the databases: master.
    /// </summary>
    public static class SystemFixed
    {
        public static ItemWrapper GetItemWrapper(Database database)
        {
            return new ItemWrapper(GetItem(database));
        }

        public static ItemWrapper ItemWrapper
        {
            get { return GetItemWrapper(Sitecore.Context.Database); }
        }

        public static ItemWrapper ItemWrapperFromMaster
        {
            get { return GetItemWrapper(Database.GetDatabase("master")); }
        }

        private static Item GetItem(Database database)
        {
            Item item = database.GetItem(ID.Parse("{13D6D6C6-C50B-4BBD-B331-2B04F1A58F21}"));
            if (item != null)
            {
                return "/sitecore/system".Equals(item.Paths.FullPath)
                           ? item
                           : (database.GetItem("/sitecore/system") ?? item);
            }
            else
            {
                return database.GetItem("/sitecore/system");
            }
        }
    }
}

#endregion

namespace Sitecore.License.Expiration.Module.RelativeFixedPaths
{
    /// <summary>
    /// Base class for fixed paths that can be used relative to other items.
    /// </summary>
    public abstract class RelativeFixedPath
    {
        /// <summary>
        /// The Sitecore item for the relative path at this point.
        /// </summary>
        public Item RelativeFixedPathItem { get; private set; }

        /// <summary>
        /// Use this constructor to create a relative fixed path tree starting with the item that is passed.
        /// </summary>
        /// <param name="relativeFixedPathItem">The item to start the tree with</param>
        public RelativeFixedPath(Item relativeFixedPathItem)
        {
            RelativeFixedPathItem = relativeFixedPathItem;
        }

        /// <summary>
        /// Because relative fixed paths cannot be validated (except for the original structure), runtime validation can be done using this method.
        /// </summary>
        /// <returns>Messages for each failed validation, or null if everything is ok</returns>
        public abstract string[] GetValidationMessages();

        protected void Validate(List<string> validationMessages, string childName, RelativeFixedPath childRelativeFixedPath)
        {
            if (childRelativeFixedPath != null)
            {
                validationMessages.AddRange(childRelativeFixedPath.GetValidationMessages() ?? new string[0]);
            }
            else
            {
                validationMessages.Add(string.Format("Could not find a child item '{0}' for relative fixed path item '{1}'.", childName, RelativeFixedPathItem.Paths.FullPath));
            }
        }

        protected void ValidateType<T>(List<string> validationMessages, Item RelativeFixedPathItem) where T : IItemWrapper
        {
            if (!typeof (T).IsAssignableFrom(ItemWrapper.CreateTypedWrapper(RelativeFixedPathItem).GetType()))
            {
                validationMessages.Add(string.Format("The item {0} was expected to be a {1}, but it was a {2}", RelativeFixedPathItem.Paths.FullPath, typeof (T).Name, RelativeFixedPathItem.TemplateName));
            }
        }
    }
}

#region Relative fixed paths from set with name "FixedPaths"

#endregion