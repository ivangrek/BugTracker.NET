namespace BugTracker.Web.Core
{
    using System;
    using Identification;
    using Microsoft.Extensions.Configuration;

    public interface IApplicationSettings
    {
        string this[string index] { get; }

        string ConnectionString { get; }

        string AbsoluteUrlPrefix { get; }

        bool LogEnabled { get; }

        bool LogSqlEnabled { get; }

        string LogFileFolder { get; }

        bool ErrorEmailEnabled { get; }

        string ErrorEmailTo { get; }

        string ErrorEmailFrom { get; }

        bool NotificationEmailEnabled { get; }

        string NotificationEmailFrom { get; }

        string NotificationSubjectFormat { get; }

        string SmtpForceSsl { get; }

        bool SmtpForceReplaceOfBareLineFeeds { get; }

        string DatepickerDateFormat { get; }

        string DateTimeFormat { get; }

        string JustDateFormat { get; }

        string SqlServerDateFormat { get; }

        bool ShowUserDefinedBugAttribute { get; }

        string UserDefinedBugAttributeName { get; }

        bool TrackBugHistory { get; }

        int DefaultPermissionLevel { get; }

        AuthenticationMode WindowsAuthentication { get; }

        bool AuthenticateUsingLdap { get; }

        string LdapServer { get; }

        string LdapUserDistinguishedName { get; }

        string LdapAuthType { get; }

        bool AllowGuestWithoutLogin { get; }

        bool EnableWindowsUserAutoRegistration { get; }

        string WindowsUserAutoRegistrationUserTemplate { get; }

        bool EnableWindowsUserAutoRegistrationLdapSearch { get; }

        string LdapDirectoryEntryPath { get; }

        string LdapDirectoryEntryAuthenticationType { get; }

        string LdapDirectoryEntryUsername { get; }

        string LdapDirectoryEntryPassword { get; }

        string LdapDirectorySearcherFilter { get; }

        string LdapFirstName { get; }

        string LdapLastName { get; }

        string LdapEmail { get; }

        string LdapEmailSignature { get; }

        int TextAreaThreshold { get; }

        int MaxTextAreaRows { get; }

        string ApplicationTitle { get; }

        string SingularBugLabel { get; }

        string PluralBugLabel { get; }

        string BugLinkMarker { get; }

        bool UseFullNames { get; }

        string CustomBugLinkLabel { get; }

        string CustomBugLinkUrl { get; }

        string CustomMenuLinkLabel { get; }

        string CustomMenuLinkUrl { get; }

        string CustomPostLinkLabel { get; }

        string CustomPostLinkUrl { get; }

        string TrackingIdString { get; }

        string AutoReplyText { get; }

        bool AutoReplyUseHtmlEmailFormat { get; }

        string SearchSql { get; }

        int SearchSuggestMinChars { get; }

        int StatusResultingFromIncomingEmail { get; }

        bool EnableInternalOnlyPosts { get; }

        bool EnableSubversionIntegration { get; }

        string SubversionPathToSvn { get; }

        string SvnHookUsername { get; }

        string SubversionAdditionalArgs { get; }

        string SvnBugidRegexPattern1 { get; }

        string SvnBugidRegexPattern2 { get; }

        bool SvnTrustPathsInUrls { get; }

        bool EnableGitIntegration { get; }

        string GitPathToGit { get; }

        string GitHookUsername { get; }

        string GitBugidRegexPattern { get; }

        bool EnableMercurialIntegration { get; }

        string MercurialPathToHg { get; }

        string MercurialHookUsername { get; }

        string MercurialBugidRegexPattern { get; }

        bool StoreAttachmentsInDatabase { get; }

        string UploadFolder { get; }

        int MaxUploadSize { get; }

        int SqlCommandCommandTimeout { get; }

        bool RequireStrongPasswords { get; }

        bool ShowForgotPasswordLink { get; }

        bool AllowSelfRegistration { get; }

        string SelfRegisteredUserTemplate { get; }

        int RegistrationExpiration { get; }

        bool ForceBordersInEmails { get; }

        bool LimitUsernameDropdownsInSearch { get; }

        bool EnableTags { get; }

        bool DisableFCKEditor { get; }

        bool UseTransmitFileInsteadOfWriteFile { get; }

        bool EnableSeen { get; }

        bool EnableVotes { get; }

        bool EnableWhatsNewPage { get; }

        int WhatsNewPageIntervalInSeconds { get; }

        int WhatsNewMaxItemsCount { get; }

        bool MemoryLogEnabled { get; }

        bool EnableLucene { get; }

        string LuceneIndexFolder { get; }

        bool DisplayAnotherButtonInEditBugPage { get; }

        bool EnableTasks { get; }

        string TaskDefaultDurationUnits { get; }

        string TaskDefaultHour { get; }

        string TaskDefaultStatus { get; }

        bool ShowTaskAssignedTo { get; }

        bool ShowTaskPlannedStartDate { get; }

        bool ShowTaskActualStartDate { get; }

        bool ShowTaskPlannedEndDate { get; }

        bool ShowTaskActualEndDate { get; }

        bool ShowTaskPlannedDuration { get; }

        bool ShowTaskActualDuration { get; }

        bool ShowTaskDurationUnits { get; }

        bool ShowTaskPercentComplete { get; }

        bool ShowTaskStatus { get; }

        bool ShowTaskSortSequence { get; }

        bool EnableRelationships { get; }

        string AspNetFormId { get; }

        string CreateUserFromEmailAddressIfThisUsername { get; }

        string CreateUsersFromEmailTemplate { get; }

        bool UseEmailDomainAsNewOrgNameWhenCreatingNewUser { get; }

        int FailedLoginAttemptsMinutes { get; }

        int FailedLoginAttemptsAllowed { get; }

        string CommentSortOrder { get; }

        int DisplayTimeOffsetInHours { get; }

        bool StripDisplayNameFromEmailAddress { get; }

        string UpdateBugAfterInsertBugAspxSql { get; }

        bool ShowPotentiallyDangerousHtml { get; }

        string EmailAddressSeparatorCharacter { get; }

        bool StripHtmlTagsFromSearchableText { get; }

        bool NoCapitalization { get; }

        bool WriteUtf8Preamble { get; }

        bool BodyEncodingUtf8 { get; }

        bool EnableEditWebConfigPage { get; }
    }

    internal sealed class ApplicationSettings : IApplicationSettings
    {
        public const string ConnectionStringDefault = " ?";
        public const string AbsoluteUrlPrefixDefault = "";
        public const bool LogEnabledDefault = true;
        public const bool LogSqlEnabledDefault = true;
        public const string LogFileFolderDefault = "App_Data\\logs";
        public const bool ErrorEmailEnabledDefault = true;
        public const string ErrorEmailToDefault = "";
        public const string ErrorEmailFromDefault = "";
        public const bool NotificationEmailEnabledDefault = true;
        public const string NotificationEmailFromDefault = "";
        public const string NotificationSubjectFormatDefault = "$THING$:$BUGID$ was $ACTION$ - $SHORTDESC$ $TRACKINGID$";
        public const string SmtpForceSslDefault = "";
        public const bool SmtpForceReplaceOfBareLineFeedsDefault = false;
        public const string DatepickerDateFormatDefault = "yy-mm-dd";
        public const string DateTimeFormatDefault = "g";
        public const string JustDateFormatDefault = "g";
        public const string SqlServerDateFormatDefault = "yyyyMMdd HH:mm:ss";
        public const bool ShowUserDefinedBugAttributeDefault = true;
        public const string UserDefinedBugAttributeNameDefault = "YourAttribute";
        public const bool TrackBugHistoryDefault = true;
        public const int DefaultPermissionLevelDefault = 2;
        public const AuthenticationMode WindowsAuthenticationDefault = AuthenticationMode.Site;
        public const bool AuthenticateUsingLdapDefault = false;
        public const string LdapServerDefault = "127.0.0.1";
        public const string LdapUserDistinguishedNameDefault = "uid=$REPLACE_WITH_USERNAME$,ou=people,dc=mycompany,dc=com";
        public const string LdapAuthTypeDefault = "Basic";
        public const bool AllowGuestWithoutLoginDefault = false;
        public const bool EnableWindowsUserAutoRegistrationDefault = false;
        public const string WindowsUserAutoRegistrationUserTemplateDefault = "guest";
        public const bool EnableWindowsUserAutoRegistrationLdapSearchDefault = false;
        public const string LdapDirectoryEntryPathDefault = "LDAP://127.0.0.1/DC=mycompany,DC=com";
        public const string LdapDirectoryEntryAuthenticationTypeDefault = "Anonymous";
        public const string LdapDirectoryEntryUsernameDefault = "";
        public const string LdapDirectoryEntryPasswordDefault = "";
        public const string LdapDirectorySearcherFilterDefault = "(uid=$REPLACE_WITH_USERNAME$)";
        public const string LdapFirstNameDefault = "gn";
        public const string LdapLastNameDefault = "sn";
        public const string LdapEmailDefault = "mail";
        public const string LdapEmailSignatureDefault = "cn";
        public const int TextAreaThresholdDefault = 80;
        public const int MaxTextAreaRowsDefault = 3;
        public const string ApplicationTitleDefault = "BugTracker.NET";
        public const string SingularBugLabelDefault = "bug";
        public const string PluralBugLabelDefault = "bugs";
        public const string BugLinkMarkerDefault = "bugid#";
        public const bool UseFullNamesDefault = false;
        public const string CustomBugLinkLabelDefault = "";
        public const string CustomBugLinkUrlDefault = "";
        public const string CustomMenuLinkLabelDefault = "";
        public const string CustomMenuLinkUrlDefault = "";
        public const string CustomPostLinkLabelDefault = "";
        public const string CustomPostLinkUrlDefault = "";
        public const string TrackingIdStringDefault = "DO NOT EDIT THIS:";
        public const string AutoReplyTextDefault = "";
        public const bool AutoReplyUseHtmlEmailFormatDefault = false;
        public const string SearchSqlDefault = "";
        public const int SearchSuggestMinCharsDefault = 3;
        public const int StatusResultingFromIncomingEmailDefault = 0;
        public const bool EnableInternalOnlyPostsDefault = false;
        public const bool EnableSubversionIntegrationDefault = false;
        public const string SubversionPathToSvnDefault = "svn";
        public const string SvnHookUsernameDefault = "";
        public const string SubversionAdditionalArgsDefault = "";
        public const string SvnBugidRegexPattern1Default = "([0-9,]+$)";
        public const string SvnBugidRegexPattern2Default = "(^[0-9,]+ )";
        public const bool SvnTrustPathsInUrlsDefault = false;
        public const bool EnableGitIntegrationDefault = false;
        public const string GitPathToGitDefault = "[path to git.exe?]";
        public const string GitHookUsernameDefault = "";
        public const string GitBugidRegexPatternDefault = "(^[0-9]+)";
        public const bool EnableMercurialIntegrationDefault = false;
        public const string MercurialPathToHgDefault = "[path to hg.exe?]";
        public const string MercurialHookUsernameDefault = "";
        public const string MercurialBugidRegexPatternDefault = "(^[0-9]+)";
        public const bool StoreAttachmentsInDatabaseDefault = false;
        public const string UploadFolderDefault = "";
        public const int MaxUploadSizeDefault = 100000;
        public const int SqlCommandCommandTimeoutDefault = 30;
        public const bool RequireStrongPasswordsDefault = false;
        public const bool ShowForgotPasswordLinkDefault = false;
        public const bool AllowSelfRegistrationDefault = false;
        public const string SelfRegisteredUserTemplateDefault = "[error - missing user template]";
        public const int RegistrationExpirationDefault = 20;
        public const bool ForceBordersInEmailsDefault = false;
        public const bool LimitUsernameDropdownsInSearchDefault = false;
        public const bool EnableTagsDefault = false;
        public const bool DisableFCKEditorDefault = false;
        public const bool UseTransmitFileInsteadOfWriteFileDefault = false;
        public const bool EnableSeenDefault = false;
        public const bool EnableVotesDefault = false;
        public const bool EnableWhatsNewPageDefault = false;
        public const int WhatsNewPageIntervalInSecondsDefault = 20;
        public const int WhatsNewMaxItemsCountDefault = 200;
        public const bool MemoryLogEnabledDefault = true;
        public const bool EnableLuceneDefault = true;
        public const string LuceneIndexFolderDefault = "App_Data\\lucene_index";
        public const bool DisplayAnotherButtonInEditBugPageDefault = false;
        public const bool EnableTasksDefault = false;
        public const string TaskDefaultDurationUnitsDefault = "hours";
        public const string TaskDefaultHourDefault = "09";
        public const string TaskDefaultStatusDefault = "[no status]";
        public const bool ShowTaskAssignedToDefault = true;
        public const bool ShowTaskPlannedStartDateDefault = true;
        public const bool ShowTaskActualStartDateDefault = true;
        public const bool ShowTaskPlannedEndDateDefault = true;
        public const bool ShowTaskActualEndDateDefault = true;
        public const bool ShowTaskPlannedDurationDefault = true;
        public const bool ShowTaskActualDurationDefault = true;
        public const bool ShowTaskDurationUnitsDefault = true;
        public const bool ShowTaskPercentCompleteDefault = true;
        public const bool ShowTaskStatusDefault = true;
        public const bool ShowTaskSortSequenceDefault = true;
        public const bool EnableRelationshipsDefault = false;
        public const string AspNetFormIdDefault = "ctl00";
        public const string CreateUserFromEmailAddressIfThisUsernameDefault = "";
        public const string CreateUsersFromEmailTemplateDefault = "[error - missing user template]";
        public const bool UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault = false;
        public const int FailedLoginAttemptsMinutesDefault = 10;
        public const int FailedLoginAttemptsAllowedDefault = 10;
        public const string CommentSortOrderDefault = "desc";
        public const int DisplayTimeOffsetInHoursDefault = 0;
        public const bool StripDisplayNameFromEmailAddressDefault = false;
        public const string UpdateBugAfterInsertBugAspxSqlDefault = "";
        public const bool ShowPotentiallyDangerousHtmlDefault = false;
        public const string EmailAddressSeparatorCharacterDefault = ",";
        public const bool StripHtmlTagsFromSearchableTextDefault = true;
        public const bool NoCapitalizationDefault = false;
        public const bool WriteUtf8PreambleDefault = true;
        public const bool BodyEncodingUtf8Default = true;
        public const bool EnableEditWebConfigPageDefault = false;

        private readonly IConfiguration configuration;

        public ApplicationSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string this[string index] => ReadOption(index, string.Empty);

        public string ConnectionString => ReadOption(nameof(ConnectionString), ConnectionStringDefault);

        public string AbsoluteUrlPrefix => ReadOption(nameof(AbsoluteUrlPrefix), AbsoluteUrlPrefixDefault);

        public bool LogEnabled => ReadOption(nameof(LogEnabled), LogEnabledDefault);

        public bool LogSqlEnabled => ReadOption(nameof(LogSqlEnabled), LogSqlEnabledDefault);

        public string LogFileFolder => ReadOption(nameof(LogFileFolder), LogFileFolderDefault);

        public bool ErrorEmailEnabled => ReadOption(nameof(ErrorEmailEnabled), ErrorEmailEnabledDefault);

        public string ErrorEmailTo => ReadOption(nameof(ErrorEmailTo), ErrorEmailToDefault);

        public string ErrorEmailFrom => ReadOption(nameof(ErrorEmailFrom), ErrorEmailFromDefault);

        public bool NotificationEmailEnabled => ReadOption(nameof(NotificationEmailEnabled), NotificationEmailEnabledDefault);

        public string NotificationEmailFrom => ReadOption(nameof(NotificationEmailFrom), NotificationEmailFromDefault);

        public string NotificationSubjectFormat => ReadOption(nameof(NotificationSubjectFormat), NotificationSubjectFormatDefault);

        public string SmtpForceSsl => ReadOption(nameof(SmtpForceSsl), SmtpForceSslDefault);

        public bool SmtpForceReplaceOfBareLineFeeds => ReadOption(nameof(SmtpForceReplaceOfBareLineFeeds), SmtpForceReplaceOfBareLineFeedsDefault);

        public string DatepickerDateFormat => ReadOption(nameof(DatepickerDateFormat), DatepickerDateFormatDefault);

        public string DateTimeFormat => ReadOption(nameof(DateTimeFormat), DateTimeFormatDefault);

        public string JustDateFormat => ReadOption(nameof(JustDateFormat), JustDateFormatDefault);

        public string SqlServerDateFormat => ReadOption(nameof(SqlServerDateFormat), SqlServerDateFormatDefault);

        public bool ShowUserDefinedBugAttribute => ReadOption(nameof(ShowUserDefinedBugAttribute), ShowUserDefinedBugAttributeDefault);

        public string UserDefinedBugAttributeName => ReadOption(nameof(UserDefinedBugAttributeName), UserDefinedBugAttributeNameDefault);

        public bool TrackBugHistory => ReadOption(nameof(TrackBugHistory), TrackBugHistoryDefault);

        public int DefaultPermissionLevel => ReadOption(nameof(DefaultPermissionLevel), DefaultPermissionLevelDefault);

        public AuthenticationMode WindowsAuthentication => ReadOption(nameof(WindowsAuthentication), WindowsAuthenticationDefault);

        public bool AuthenticateUsingLdap => ReadOption(nameof(AuthenticateUsingLdap), AuthenticateUsingLdapDefault);

        public string LdapServer => ReadOption(nameof(LdapServer), LdapServerDefault);

        public string LdapUserDistinguishedName => ReadOption(nameof(LdapUserDistinguishedName), LdapUserDistinguishedNameDefault);

        public string LdapAuthType => ReadOption(nameof(LdapAuthType), LdapAuthTypeDefault);

        public bool AllowGuestWithoutLogin => ReadOption(nameof(AllowGuestWithoutLogin), AllowGuestWithoutLoginDefault);

        public bool EnableWindowsUserAutoRegistration => ReadOption(nameof(EnableWindowsUserAutoRegistration), EnableWindowsUserAutoRegistrationDefault);

        public string WindowsUserAutoRegistrationUserTemplate => ReadOption(nameof(WindowsUserAutoRegistrationUserTemplate), WindowsUserAutoRegistrationUserTemplateDefault);

        public bool EnableWindowsUserAutoRegistrationLdapSearch => ReadOption(nameof(EnableWindowsUserAutoRegistrationLdapSearch), EnableWindowsUserAutoRegistrationLdapSearchDefault);

        public string LdapDirectoryEntryPath => ReadOption(nameof(LdapDirectoryEntryPath), LdapDirectoryEntryPathDefault);

        public string LdapDirectoryEntryAuthenticationType => ReadOption(nameof(LdapDirectoryEntryAuthenticationType), LdapDirectoryEntryAuthenticationTypeDefault);

        public string LdapDirectoryEntryUsername => ReadOption(nameof(LdapDirectoryEntryUsername), LdapDirectoryEntryUsernameDefault);

        public string LdapDirectoryEntryPassword => ReadOption(nameof(LdapDirectoryEntryPassword), LdapDirectoryEntryPasswordDefault);

        public string LdapDirectorySearcherFilter => ReadOption(nameof(LdapDirectorySearcherFilter), LdapDirectorySearcherFilterDefault);

        public string LdapFirstName => ReadOption(nameof(LdapFirstName), LdapFirstNameDefault);

        public string LdapLastName => ReadOption(nameof(LdapLastName), LdapLastNameDefault);

        public string LdapEmail => ReadOption(nameof(LdapEmail), LdapEmailDefault);

        public string LdapEmailSignature => ReadOption(nameof(LdapEmailSignature), LdapEmailSignatureDefault);

        public int TextAreaThreshold => ReadOption(nameof(TextAreaThreshold), TextAreaThresholdDefault);

        public int MaxTextAreaRows => ReadOption(nameof(MaxTextAreaRows), MaxTextAreaRowsDefault);

        public string ApplicationTitle => ReadOption(nameof(ApplicationTitle), ApplicationTitleDefault);

        public string SingularBugLabel => ReadOption(nameof(SingularBugLabel), SingularBugLabelDefault);

        public string PluralBugLabel => ReadOption(nameof(PluralBugLabel), PluralBugLabelDefault);

        public string BugLinkMarker => ReadOption(nameof(BugLinkMarker), BugLinkMarkerDefault);

        public bool UseFullNames => ReadOption(nameof(UseFullNames), UseFullNamesDefault);

        public string CustomBugLinkLabel => ReadOption(nameof(CustomBugLinkLabel), CustomBugLinkLabelDefault);

        public string CustomBugLinkUrl => ReadOption(nameof(CustomBugLinkUrl), CustomBugLinkUrlDefault);

        public string CustomMenuLinkLabel => ReadOption(nameof(CustomMenuLinkLabel), CustomMenuLinkLabelDefault);

        public string CustomMenuLinkUrl => ReadOption(nameof(CustomMenuLinkUrl), CustomMenuLinkUrlDefault);

        public string CustomPostLinkLabel => ReadOption(nameof(CustomPostLinkLabel), CustomPostLinkLabelDefault);

        public string CustomPostLinkUrl => ReadOption(nameof(CustomPostLinkUrl), CustomPostLinkUrlDefault);

        public string TrackingIdString => ReadOption(nameof(TrackingIdString), TrackingIdStringDefault);

        public string AutoReplyText => ReadOption(nameof(AutoReplyText), AutoReplyTextDefault);

        public bool AutoReplyUseHtmlEmailFormat => ReadOption(nameof(AutoReplyUseHtmlEmailFormat), AutoReplyUseHtmlEmailFormatDefault);

        public string SearchSql => ReadOption(nameof(SearchSql), SearchSqlDefault);

        public int SearchSuggestMinChars => ReadOption(nameof(SearchSuggestMinChars), SearchSuggestMinCharsDefault);

        public int StatusResultingFromIncomingEmail => ReadOption(nameof(StatusResultingFromIncomingEmail), StatusResultingFromIncomingEmailDefault);

        public bool EnableInternalOnlyPosts => ReadOption(nameof(EnableInternalOnlyPosts), EnableInternalOnlyPostsDefault);

        public bool EnableSubversionIntegration => ReadOption(nameof(EnableSubversionIntegration), EnableSubversionIntegrationDefault);

        public string SubversionPathToSvn => ReadOption(nameof(SubversionPathToSvn), SubversionPathToSvnDefault);

        public string SvnHookUsername => ReadOption(nameof(SvnHookUsername), SvnHookUsernameDefault);

        public string SubversionAdditionalArgs => ReadOption(nameof(SubversionAdditionalArgs), SubversionAdditionalArgsDefault);

        public string SvnBugidRegexPattern1 => ReadOption(nameof(SvnBugidRegexPattern1), SvnBugidRegexPattern1Default);

        public string SvnBugidRegexPattern2 => ReadOption(nameof(SvnBugidRegexPattern2), SvnBugidRegexPattern2Default);

        public bool SvnTrustPathsInUrls => ReadOption(nameof(SvnTrustPathsInUrls), SvnTrustPathsInUrlsDefault);

        public bool EnableGitIntegration => ReadOption(nameof(EnableGitIntegration), EnableGitIntegrationDefault);

        public string GitPathToGit => ReadOption(nameof(GitPathToGit), GitPathToGitDefault);

        public string GitHookUsername => ReadOption(nameof(GitHookUsername), GitHookUsernameDefault);

        public string GitBugidRegexPattern => ReadOption(nameof(GitBugidRegexPattern), GitBugidRegexPatternDefault);

        public bool EnableMercurialIntegration => ReadOption(nameof(EnableMercurialIntegration), EnableMercurialIntegrationDefault);

        public string MercurialPathToHg => ReadOption(nameof(MercurialPathToHg), MercurialPathToHgDefault);

        public string MercurialHookUsername => ReadOption(nameof(MercurialHookUsername), MercurialHookUsernameDefault);

        public string MercurialBugidRegexPattern => ReadOption(nameof(MercurialBugidRegexPattern), MercurialBugidRegexPatternDefault);

        public bool StoreAttachmentsInDatabase => ReadOption(nameof(StoreAttachmentsInDatabase), StoreAttachmentsInDatabaseDefault);

        public string UploadFolder => ReadOption(nameof(UploadFolder), UploadFolderDefault);

        public int MaxUploadSize => ReadOption(nameof(MaxUploadSize), MaxUploadSizeDefault);

        public int SqlCommandCommandTimeout => ReadOption("SqlCommand.CommandTimeout", SqlCommandCommandTimeoutDefault);

        public bool RequireStrongPasswords => ReadOption(nameof(RequireStrongPasswords), RequireStrongPasswordsDefault);

        public bool ShowForgotPasswordLink => ReadOption(nameof(ShowForgotPasswordLink), ShowForgotPasswordLinkDefault);

        public bool AllowSelfRegistration => ReadOption(nameof(AllowSelfRegistration), AllowSelfRegistrationDefault);

        public string SelfRegisteredUserTemplate => ReadOption(nameof(SelfRegisteredUserTemplate), SelfRegisteredUserTemplateDefault);

        public int RegistrationExpiration => ReadOption(nameof(RegistrationExpiration), RegistrationExpirationDefault);

        public bool ForceBordersInEmails => ReadOption(nameof(ForceBordersInEmails), ForceBordersInEmailsDefault);

        public bool LimitUsernameDropdownsInSearch => ReadOption(nameof(LimitUsernameDropdownsInSearch), LimitUsernameDropdownsInSearchDefault);

        public bool EnableTags => ReadOption(nameof(EnableTags), EnableTagsDefault);

        public bool DisableFCKEditor => ReadOption(nameof(DisableFCKEditor), DisableFCKEditorDefault);

        public bool UseTransmitFileInsteadOfWriteFile => ReadOption(nameof(UseTransmitFileInsteadOfWriteFile), UseTransmitFileInsteadOfWriteFileDefault);

        public bool EnableSeen => ReadOption(nameof(EnableSeen), EnableSeenDefault);

        public bool EnableVotes => ReadOption(nameof(EnableVotes), EnableVotesDefault);

        public bool EnableWhatsNewPage => ReadOption(nameof(EnableWhatsNewPage), EnableWhatsNewPageDefault);

        public int WhatsNewPageIntervalInSeconds => ReadOption(nameof(WhatsNewPageIntervalInSeconds), WhatsNewPageIntervalInSecondsDefault);

        public int WhatsNewMaxItemsCount => ReadOption(nameof(WhatsNewMaxItemsCount), WhatsNewMaxItemsCountDefault);

        public bool MemoryLogEnabled => ReadOption(nameof(MemoryLogEnabled), MemoryLogEnabledDefault);

        public bool EnableLucene => ReadOption(nameof(EnableLucene), EnableLuceneDefault);

        public string LuceneIndexFolder => ReadOption(nameof(LuceneIndexFolder), LuceneIndexFolderDefault);

        public bool DisplayAnotherButtonInEditBugPage => ReadOption(nameof(DisplayAnotherButtonInEditBugPage), DisplayAnotherButtonInEditBugPageDefault);

        public bool EnableTasks => ReadOption(nameof(EnableTasks), EnableTasksDefault);

        public string TaskDefaultDurationUnits => ReadOption(nameof(TaskDefaultDurationUnits), TaskDefaultDurationUnitsDefault);

        public string TaskDefaultHour => ReadOption(nameof(TaskDefaultHour), TaskDefaultHourDefault);

        public string TaskDefaultStatus => ReadOption(nameof(TaskDefaultStatus), TaskDefaultStatusDefault);

        public bool ShowTaskAssignedTo => ReadOption(nameof(ShowTaskAssignedTo), ShowTaskAssignedToDefault);

        public bool ShowTaskPlannedStartDate => ReadOption(nameof(ShowTaskPlannedStartDate), ShowTaskPlannedStartDateDefault);

        public bool ShowTaskActualStartDate => ReadOption(nameof(ShowTaskActualStartDate), ShowTaskActualStartDateDefault);

        public bool ShowTaskPlannedEndDate => ReadOption(nameof(ShowTaskPlannedEndDate), ShowTaskPlannedEndDateDefault);

        public bool ShowTaskActualEndDate => ReadOption(nameof(ShowTaskActualEndDate), ShowTaskActualEndDateDefault);

        public bool ShowTaskPlannedDuration => ReadOption(nameof(ShowTaskPlannedDuration), ShowTaskPlannedDurationDefault);

        public bool ShowTaskActualDuration => ReadOption(nameof(ShowTaskActualDuration), ShowTaskActualDurationDefault);

        public bool ShowTaskDurationUnits => ReadOption(nameof(ShowTaskDurationUnits), ShowTaskDurationUnitsDefault);

        public bool ShowTaskPercentComplete => ReadOption(nameof(ShowTaskPercentComplete), ShowTaskPercentCompleteDefault);

        public bool ShowTaskStatus => ReadOption(nameof(ShowTaskStatus), ShowTaskStatusDefault);

        public bool ShowTaskSortSequence => ReadOption(nameof(ShowTaskSortSequence), ShowTaskSortSequenceDefault);

        public bool EnableRelationships => ReadOption(nameof(EnableRelationships), EnableRelationshipsDefault);

        public string AspNetFormId => ReadOption(nameof(AspNetFormId), AspNetFormIdDefault);

        public string CreateUserFromEmailAddressIfThisUsername => ReadOption(nameof(CreateUserFromEmailAddressIfThisUsername), CreateUserFromEmailAddressIfThisUsernameDefault);

        public string CreateUsersFromEmailTemplate => ReadOption(nameof(CreateUsersFromEmailTemplate), CreateUsersFromEmailTemplateDefault);

        public bool UseEmailDomainAsNewOrgNameWhenCreatingNewUser => ReadOption(nameof(UseEmailDomainAsNewOrgNameWhenCreatingNewUser), UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault);

        public int FailedLoginAttemptsMinutes => ReadOption(nameof(FailedLoginAttemptsMinutes), FailedLoginAttemptsMinutesDefault);

        public int FailedLoginAttemptsAllowed => ReadOption(nameof(FailedLoginAttemptsAllowed), FailedLoginAttemptsAllowedDefault);

        public string CommentSortOrder => ReadOption(nameof(CommentSortOrder), CommentSortOrderDefault);

        public int DisplayTimeOffsetInHours => ReadOption(nameof(DisplayTimeOffsetInHours), DisplayTimeOffsetInHoursDefault);

        public bool StripDisplayNameFromEmailAddress => ReadOption(nameof(StripDisplayNameFromEmailAddress), StripDisplayNameFromEmailAddressDefault);

        public string UpdateBugAfterInsertBugAspxSql => ReadOption(nameof(UpdateBugAfterInsertBugAspxSql), UpdateBugAfterInsertBugAspxSqlDefault);

        public bool ShowPotentiallyDangerousHtml => ReadOption(nameof(ShowPotentiallyDangerousHtml), ShowPotentiallyDangerousHtmlDefault);

        public string EmailAddressSeparatorCharacter => ReadOption(nameof(EmailAddressSeparatorCharacter), EmailAddressSeparatorCharacterDefault);

        public bool EnableEditWebConfigPage => ReadOption(nameof(EnableEditWebConfigPage), EnableEditWebConfigPageDefault);

        public bool StripHtmlTagsFromSearchableText => ReadOption(nameof(StripHtmlTagsFromSearchableText), StripHtmlTagsFromSearchableTextDefault);

        public bool NoCapitalization => ReadOption(nameof(NoCapitalization), NoCapitalizationDefault);

        public bool WriteUtf8Preamble => ReadOption(nameof(WriteUtf8Preamble), WriteUtf8PreambleDefault);

        public bool BodyEncodingUtf8 => ReadOption(nameof(BodyEncodingUtf8), BodyEncodingUtf8Default);

        private TValue ReadOption<TValue>(string name, TValue defaultValue)
            where TValue : IConvertible
        {
            var section = this.configuration.GetSection(nameof(ApplicationSettings));
            var value = section[name];

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            if (typeof(TValue).IsEnum)
            {
                return (TValue)Enum.Parse(typeof(TValue), value);
            }

            return (TValue)Convert.ChangeType(value, typeof(TValue));
        }
    }
}
