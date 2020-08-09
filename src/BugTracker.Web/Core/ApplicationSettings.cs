namespace BugTracker.Web.Core
{
    using System;
    using System.Configuration;
    using Identification;

    public interface IApplicationSettings
    {
        string this[string index] { get; }

        AuthenticationMode WindowsAuthentication { get; }

        bool AllowGuestWithoutLogin { get; }

        bool AllowSelfRegistration { get; }

        bool ShowForgotPasswordLink { get; }

        string AppTitle { get; }

        int RegistrationExpiration { get; }

        string SelfRegisteredUserTemplate { get; }

        string NotificationEmailFrom { get; }

        string AbsoluteUrlPrefix { get; }

        string ConnectionString { get; }

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

        bool EnableMobile { get; }

        string PluralBugLabel { get; }

        bool UseTransmitFileInsteadOfWriteFile { get; }

        int DefaultPermissionLevel { get; }

        bool EnableInternalOnlyPosts { get; }

        int MaxUploadSize { get; }

        string SingularBugLabel { get; }

        bool DisplayAnotherButtonInEditBugPage { get; }

        bool EnableRelationships { get; }

        bool EnableSubversionIntegration { get; }

        bool EnableGitIntegration { get; }

        bool EnableMercurialIntegration { get; }

        bool EnableTasks { get; }

        bool EnableTags { get; }

        string CustomBugLinkLabel { get; }

        string CustomBugLinkUrl { get; }

        bool UseFullNames { get; }

        bool ShowUserDefinedBugAttribute { get; }

        bool TrackBugHistory { get; }

        bool EnableWhatsNewPage { get; }

        bool NotificationEmailEnabled { get; }

        string UserDefinedBugAttributeName { get; }

        int TextAreaThreshold { get; }

        int MaxTextAreaRows { get; }

        string BugLinkMarker { get; }

        bool DisableFCKEditor { get; }

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

        string TaskDefaultDurationUnits { get; }

        string TaskDefaultHour { get; }

        string TaskDefaultStatus { get; }

        bool StripDisplayNameFromEmailAddress { get; }

        string TrackingIdString { get; }

        string SearchSQL { get; }

        string AspNetFormId { get; }

        bool LogEnabled { get; }

        bool StripHtmlTagsFromSearchableText { get; }

        string JustDateFormat { get; }

        string DateTimeFormat { get; }

        int DisplayTimeOffsetInHours { get; }

        bool EnableVotes { get; }

        bool EnableLucene { get; }

        bool EnablePop3 { get; }

        bool ErrorEmailEnabled { get; }

        string ErrorEmailTo { get; }

        string ErrorEmailFrom { get; }

        bool MemoryLogEnabled { get; }

        bool SvnTrustPathsInUrls { get; }

        int WhatsNewMaxItemsCount { get; }

        string GitHookUsername { get; }

        string GitBugidRegexPattern { get; }

        string SvnHookUsername { get; }

        string SvnBugidRegexPattern1 { get; }

        string SvnBugidRegexPattern2 { get; }

        string MercurialHookUsername { get; }

        string MercurialBugidRegexPattern { get; }

        string GitPathToGit { get; }

        string MercurialPathToHg { get; }

        string SubversionPathToSvn { get; }

        string SubversionAdditionalArgs { get; }

        string SQLServerDateFormat { get; }

        bool NoCapitalization { get; }

        bool LimitUsernameDropdownsInSearch { get; }

        bool RequireStrongPasswords { get; }

        bool WriteUtf8Preamble { get; }

        bool ForceBordersInEmails { get; }

        string CustomPostLinkLabel { get; }

        string CustomPostLinkUrl { get; }

        bool ShowPotentiallyDangerousHtml { get; }

        string CommentSortOrder { get; }

        bool StoreAttachmentsInDatabase { get; }

        int FailedLoginAttemptsMinutes { get; }

        int FailedLoginAttemptsAllowed { get; }

        int StatusResultingFromIncomingEmail { get; }

        bool AuthenticateUsingLdap { get; }

        string LdapUserDistinguishedName { get; }

        string LdapServer { get; }

        string LdapAuthType { get; }

        bool LogSqlEnabled { get; }

        bool BodyEncodingUTF8 { get; }

        bool SmtpForceReplaceOfBareLineFeeds { get; }

        string SmtpForceSsl { get; }

        string EmailAddressSeparatorCharacter { get; }

        string Pop3Server { get; }

        int Pop3Port { get; }

        bool Pop3UseSSL { get; }

        string Pop3ServiceUsername { get; }

        int Pop3TotalErrorsAllowed { get; }

        bool Pop3ReadInputStreamCharByChar { get; }

        string Pop3SubjectMustContain { get; }

        string Pop3SubjectCannotContain { get; }

        string Pop3FromMustContain { get; }

        string Pop3FromCannotContain { get; }

        bool Pop3DeleteMessagesOnServer { get; }

        bool Pop3WriteRawMessagesToLog { get; }

        int Pop3FetchIntervalInMinutes { get; }

        string AutoReplyText { get; }

        bool AutoReplyUseHtmlEmailFormat { get; }

        string CreateUserFromEmailAddressIfThisUsername { get; }

        bool UseEmailDomainAsNewOrgNameWhenCreatingNewUser { get; }

        string CreateUsersFromEmailTemplate { get; }

        int SqlCommandCommandTimeout { get; }

        bool EnableSeen { get; }

        string UpdateBugAfterInsertBugAspxSql { get; }

        string NotificationSubjectFormat { get; }

        string CustomMenuLinkLabel { get; }

        string CustomMenuLinkUrl { get; }

        int SearchSuggestMinChars { get; }

        int WhatsNewPageIntervalInSeconds { get; }

        string DatepickerDateFormat { get; }

        bool EnableEditWebConfigPage { get; }
    }

    internal sealed class ApplicationSettings : IApplicationSettings
    {
        public const AuthenticationMode WindowsAuthenticationDefault = AuthenticationMode.Site;
        public const bool AllowGuestWithoutLoginDefault = false;
        public const bool AllowSelfRegistrationDefault = false;
        public const bool ShowForgotPasswordLinkDefault = false;
        public const string AppTitleDefault = "BugTracker.NET";
        public const int RegistrationExpirationDefault = 20;
        public const string SelfRegisteredUserTemplateDefault = "[error - missing user template]";
        public const string NotificationEmailFromDefault = "";
        public const string AbsoluteUrlPrefixDefault = "";
        public const string ConnectionStringDefault = " ?";
        public const bool EnableWindowsUserAutoRegistrationDefault = true;
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
        public const bool EnableMobileDefault = false;
        public const string SingularBugLabelDefault = "bug";
        public const string PluralBugLabelDefault = "bugs";
        public const bool UseTransmitFileInsteadOfWriteFileDefault = false;
        public const int DefaultPermissionLevelDefault = 2;
        public const bool EnableInternalOnlyPostsDefault = false;
        public const int MaxUploadSizeDefault = 100000;
        public const bool DisplayAnotherButtonInEditBugPageDefault = false;
        public const bool EnableRelationshipsDefault = false;
        public const bool EnableSubversionIntegrationDefault = false;
        public const bool EnableGitIntegrationDefault = false;
        public const bool EnableMercurialIntegrationDefault = false;
        public const bool EnableTasksDefault = false;
        public const bool EnableTagsDefault = false;
        public const string CustomBugLinkLabelDefault = "";
        public const string CustomBugLinkUrlDefault = "";
        public const bool UseFullNamesDefault = false;
        public const bool ShowUserDefinedBugAttributeDefault = true;
        public const bool TrackBugHistoryDefault = true;
        public const bool EnableWhatsNewPageDefault = false;
        public const bool NotificationEmailEnabledDefault = true;
        public const string UserDefinedBugAttributeNameDefault = "YOUR ATTRIBUTE";
        public const int TextAreaThresholdDefault = 100;
        public const int MaxTextAreaRowsDefault = 5;
        public const string BugLinkMarkerDefault = "bugid#";
        public const bool DisableFCKEditorDefault = false;
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
        public const string TaskDefaultDurationUnitsDefault = "hours";
        public const string TaskDefaultHourDefault = "09";
        public const string TaskDefaultStatusDefault = "[no status]";
        public const bool StripDisplayNameFromEmailAddressDefault = false;
        public const string TrackingIdStringDefault = "DO NOT EDIT THIS:";
        public const string SearchSQLDefault = "";
        public const string AspNetFormIdDefault = "ctl00";
        public const bool LogEnabledDefault = true;
        public const bool StripHtmlTagsFromSearchableTextDefault = true;
        public const string JustDateFormatDefault = "g";
        public const string DateTimeFormatDefault = "g";
        public const int DisplayTimeOffsetInHoursDefault = 0;
        public const bool EnableVotesDefault = false;
        public const bool EnableLuceneDefault = true;
        public const bool EnablePop3Default = false;
        public const bool ErrorEmailEnabledDefault = true;
        public const string ErrorEmailToDefault = "";
        public const string ErrorEmailFromDefault = "";
        public const bool MemoryLogEnabledDefault = true;
        public const bool SvnTrustPathsInUrlsDefault = false;
        public const int WhatsNewMaxItemsCountDefault = 200;
        public const string GitHookUsernameDefault = "";
        public const string GitBugidRegexPatternDefault = "(^[0-9]+)";
        public const string SvnHookUsernameDefault = "";
        public const string SvnBugidRegexPattern1Default = "([0-9,]+$)";
        public const string SvnBugidRegexPattern2Default = "(^[0-9,]+ )";
        public const string MercurialHookUsernameDefault = "";
        public const string MercurialBugidRegexPatternDefault = "(^[0-9]+)";
        public const string GitPathToGitDefault = "[path to git.exe?]";
        public const string MercurialPathToHgDefault = "[path to hg.exe?]";
        public const string SubversionPathToSvnDefault = "svn";
        public const string SubversionAdditionalArgsDefault = "";
        public const string SQLServerDateFormatDefault = "yyyyMMdd HH:mm:ss";
        public const bool NoCapitalizationDefault = false;
        public const bool LimitUsernameDropdownsInSearchDefault = false;
        public const bool RequireStrongPasswordsDefault = false;
        public const bool WriteUtf8PreambleDefault = true;
        public const bool ForceBordersInEmailsDefault = false;
        public const string CustomPostLinkLabelDefault = "";
        public const string CustomPostLinkUrlDefault = "";
        public const bool ShowPotentiallyDangerousHtmlDefault = false;
        public const string CommentSortOrderDefault = "desc";
        public const bool StoreAttachmentsInDatabaseDefault = false;
        public const int FailedLoginAttemptsMinutesDefault = 10;
        public const int FailedLoginAttemptsAllowedDefault = 10;
        public const int StatusResultingFromIncomingEmailDefault = 0;
        public const bool AuthenticateUsingLdapDefault = false;
        public const string LdapUserDistinguishedNameDefault = "uid=$REPLACE_WITH_USERNAME$,ou=people,dc=mycompany,dc=com";
        public const string LdapServerDefault = "127.0.0.1";
        public const string LdapAuthTypeDefault = "Basic";
        public const bool LogSqlEnabledDefault = true;
        public const bool BodyEncodingUTF8Default = true;
        public const bool SmtpForceReplaceOfBareLineFeedsDefault = false;
        public const string SmtpForceSslDefault = "";
        public const string EmailAddressSeparatorCharacterDefault = ",";
        public const string Pop3ServerDefault = "pop.gmail.com";
        public const int Pop3PortDefault = 995;
        public const bool Pop3UseSSLDefault = true;
        public const string Pop3ServiceUsernameDefault = "admin";
        public const int Pop3TotalErrorsAllowedDefault = 100;
        public const bool Pop3ReadInputStreamCharByCharDefault = false;
        public const string Pop3SubjectMustContainDefault = "";
        public const string Pop3SubjectCannotContainDefault = "";
        public const string Pop3FromMustContainDefault = "";
        public const string Pop3FromCannotContainDefault = "";
        public const bool Pop3DeleteMessagesOnServerDefault = false;
        public const bool Pop3WriteRawMessagesToLogDefault = false;
        public const int Pop3FetchIntervalInMinutesDefault = 15;
        public const string AutoReplyTextDefault = "";
        public const bool AutoReplyUseHtmlEmailFormatDefault = false;
        public const string CreateUserFromEmailAddressIfThisUsernameDefault = "";
        public const bool UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault = false;
        public const string CreateUsersFromEmailTemplateDefault = "[error - missing user template]";
        public const int SqlCommandCommandTimeoutDefault = 30;
        public const bool EnableSeenDefault = false;
        public const string UpdateBugAfterInsertBugAspxSqlDefault = "";
        public const string NotificationSubjectFormatDefault = "$THING$:$BUGID$ was $ACTION$ - $SHORTDESC$ $TRACKINGID$";
        public const string CustomMenuLinkLabelDefault = "";
        public const string CustomMenuLinkUrlDefault = "";
        public const int SearchSuggestMinCharsDefault = 3;
        public const int WhatsNewPageIntervalInSecondsDefault = 20;
        public const string DatepickerDateFormatDefault = "yy-mm-dd";
        public const bool EnableEditWebConfigPageDefault = false;

        public string this[string index] => ReadSetting(index, string.Empty);

        public AuthenticationMode WindowsAuthentication => ReadSetting(nameof(WindowsAuthentication), WindowsAuthenticationDefault);

        public bool AllowGuestWithoutLogin => ReadSetting(nameof(AllowGuestWithoutLogin), AllowGuestWithoutLoginDefault);

        public bool AllowSelfRegistration => ReadSetting(nameof(AllowSelfRegistration), AllowSelfRegistrationDefault);

        public bool ShowForgotPasswordLink => ReadSetting(nameof(ShowForgotPasswordLink), ShowForgotPasswordLinkDefault);

        public string AppTitle => ReadSetting(nameof(AppTitle), AppTitleDefault);

        public int RegistrationExpiration => ReadSetting(nameof(RegistrationExpiration), RegistrationExpirationDefault);

        public string SelfRegisteredUserTemplate => ReadSetting(nameof(SelfRegisteredUserTemplate), SelfRegisteredUserTemplateDefault);

        public string NotificationEmailFrom => ReadSetting(nameof(NotificationEmailFrom), NotificationEmailFromDefault);

        public string AbsoluteUrlPrefix => ReadSetting(nameof(AbsoluteUrlPrefix), AbsoluteUrlPrefixDefault);

        public string ConnectionString => ReadSetting(nameof(ConnectionString), ConnectionStringDefault);

        public bool EnableWindowsUserAutoRegistration => ReadSetting(nameof(EnableWindowsUserAutoRegistration), EnableWindowsUserAutoRegistrationDefault);

        public string WindowsUserAutoRegistrationUserTemplate => ReadSetting(nameof(WindowsUserAutoRegistrationUserTemplate), WindowsUserAutoRegistrationUserTemplateDefault);

        public bool EnableWindowsUserAutoRegistrationLdapSearch => ReadSetting(nameof(EnableWindowsUserAutoRegistrationLdapSearch), EnableWindowsUserAutoRegistrationLdapSearchDefault);

        public string LdapDirectoryEntryPath => ReadSetting(nameof(LdapDirectoryEntryPath), LdapDirectoryEntryPathDefault);

        public string LdapDirectoryEntryAuthenticationType => ReadSetting(nameof(LdapDirectoryEntryAuthenticationType), LdapDirectoryEntryAuthenticationTypeDefault);

        public string LdapDirectoryEntryUsername => ReadSetting(nameof(LdapDirectoryEntryUsername), LdapDirectoryEntryUsernameDefault);

        public string LdapDirectoryEntryPassword => ReadSetting(nameof(LdapDirectoryEntryPassword), LdapDirectoryEntryPasswordDefault);

        public string LdapDirectorySearcherFilter => ReadSetting(nameof(LdapDirectorySearcherFilter), LdapDirectorySearcherFilterDefault);

        public string LdapFirstName => ReadSetting(nameof(LdapFirstName), LdapFirstNameDefault);

        public string LdapLastName => ReadSetting(nameof(LdapLastName), LdapLastNameDefault);

        public string LdapEmail => ReadSetting(nameof(LdapEmail), LdapEmailDefault);

        public string LdapEmailSignature => ReadSetting(nameof(LdapEmailSignature), LdapEmailSignatureDefault);

        public bool EnableMobile => ReadSetting(nameof(EnableMobile), EnableMobileDefault);

        public string SingularBugLabel => ReadSetting(nameof(SingularBugLabel), SingularBugLabelDefault);

        public string PluralBugLabel => ReadSetting(nameof(PluralBugLabel), PluralBugLabelDefault);

        public bool UseTransmitFileInsteadOfWriteFile => ReadSetting(nameof(UseTransmitFileInsteadOfWriteFile), UseTransmitFileInsteadOfWriteFileDefault);

        public int DefaultPermissionLevel => ReadSetting(nameof(DefaultPermissionLevel), DefaultPermissionLevelDefault);

        public bool EnableInternalOnlyPosts => ReadSetting(nameof(EnableInternalOnlyPosts), EnableInternalOnlyPostsDefault);

        public int MaxUploadSize => ReadSetting(nameof(MaxUploadSize), MaxUploadSizeDefault);

        public bool DisplayAnotherButtonInEditBugPage => ReadSetting(nameof(DisplayAnotherButtonInEditBugPage), DisplayAnotherButtonInEditBugPageDefault);

        public bool EnableRelationships => ReadSetting(nameof(EnableRelationships), EnableRelationshipsDefault);

        public bool EnableSubversionIntegration => ReadSetting(nameof(EnableSubversionIntegration), EnableSubversionIntegrationDefault);

        public bool EnableGitIntegration => ReadSetting(nameof(EnableGitIntegration), EnableGitIntegrationDefault);

        public bool EnableMercurialIntegration => ReadSetting(nameof(EnableMercurialIntegration), EnableMercurialIntegrationDefault);

        public bool EnableTasks => ReadSetting(nameof(EnableTasks), EnableTasksDefault);

        public bool EnableTags => ReadSetting(nameof(EnableTags), EnableTagsDefault);

        public string CustomBugLinkLabel => ReadSetting(nameof(CustomBugLinkLabel), CustomBugLinkLabelDefault);

        public string CustomBugLinkUrl => ReadSetting(nameof(CustomBugLinkUrl), CustomBugLinkUrlDefault);

        public bool UseFullNames => ReadSetting(nameof(UseFullNames), UseFullNamesDefault);

        public bool ShowUserDefinedBugAttribute => ReadSetting(nameof(ShowUserDefinedBugAttribute), ShowUserDefinedBugAttributeDefault);

        public bool TrackBugHistory => ReadSetting(nameof(TrackBugHistory), TrackBugHistoryDefault);

        public bool EnableWhatsNewPage => ReadSetting(nameof(EnableWhatsNewPage), EnableWhatsNewPageDefault);

        public bool NotificationEmailEnabled => ReadSetting(nameof(NotificationEmailEnabled), NotificationEmailEnabledDefault);

        public string UserDefinedBugAttributeName => ReadSetting(nameof(UserDefinedBugAttributeName), UserDefinedBugAttributeNameDefault);

        public int TextAreaThreshold => ReadSetting(nameof(TextAreaThreshold), TextAreaThresholdDefault);

        public int MaxTextAreaRows => ReadSetting(nameof(MaxTextAreaRows), MaxTextAreaRowsDefault);

        public string BugLinkMarker => ReadSetting(nameof(BugLinkMarker), BugLinkMarkerDefault);

        public bool DisableFCKEditor => ReadSetting(nameof(DisableFCKEditor), DisableFCKEditorDefault);

        public bool ShowTaskAssignedTo => ReadSetting(nameof(ShowTaskAssignedTo), ShowTaskAssignedToDefault);

        public bool ShowTaskPlannedStartDate => ReadSetting(nameof(ShowTaskPlannedStartDate), ShowTaskPlannedStartDateDefault);

        public bool ShowTaskActualStartDate => ReadSetting(nameof(ShowTaskActualStartDate), ShowTaskActualStartDateDefault);

        public bool ShowTaskPlannedEndDate => ReadSetting(nameof(ShowTaskPlannedEndDate), ShowTaskPlannedEndDateDefault);

        public bool ShowTaskActualEndDate => ReadSetting(nameof(ShowTaskActualEndDate), ShowTaskActualEndDateDefault);

        public bool ShowTaskPlannedDuration => ReadSetting(nameof(ShowTaskPlannedDuration), ShowTaskPlannedDurationDefault);

        public bool ShowTaskActualDuration => ReadSetting(nameof(ShowTaskActualDuration), ShowTaskActualDurationDefault);

        public bool ShowTaskDurationUnits => ReadSetting(nameof(ShowTaskDurationUnits), ShowTaskDurationUnitsDefault);

        public bool ShowTaskPercentComplete => ReadSetting(nameof(ShowTaskPercentComplete), ShowTaskPercentCompleteDefault);

        public bool ShowTaskStatus => ReadSetting(nameof(ShowTaskStatus), ShowTaskStatusDefault);

        public bool ShowTaskSortSequence => ReadSetting(nameof(ShowTaskSortSequence), ShowTaskSortSequenceDefault);

        public string TaskDefaultDurationUnits => ReadSetting(nameof(TaskDefaultDurationUnits), TaskDefaultDurationUnitsDefault);

        public string TaskDefaultHour => ReadSetting(nameof(TaskDefaultHour), TaskDefaultHourDefault);

        public string TaskDefaultStatus => ReadSetting(nameof(TaskDefaultStatus), TaskDefaultStatusDefault);

        public bool StripDisplayNameFromEmailAddress => ReadSetting(nameof(StripDisplayNameFromEmailAddress), StripDisplayNameFromEmailAddressDefault);

        public string TrackingIdString => ReadSetting(nameof(TrackingIdString), TrackingIdStringDefault);

        public string SearchSQL => ReadSetting(nameof(SearchSQL), SearchSQLDefault);

        public string AspNetFormId => ReadSetting(nameof(AspNetFormId), AspNetFormIdDefault);

        public bool LogEnabled => ReadSetting(nameof(LogEnabled), LogEnabledDefault);

        public bool StripHtmlTagsFromSearchableText => ReadSetting(nameof(StripHtmlTagsFromSearchableText), StripHtmlTagsFromSearchableTextDefault);

        public string JustDateFormat => ReadSetting(nameof(JustDateFormat), JustDateFormatDefault);

        public string DateTimeFormat => ReadSetting(nameof(DateTimeFormat), DateTimeFormatDefault);

        public int DisplayTimeOffsetInHours => ReadSetting(nameof(DisplayTimeOffsetInHours), DisplayTimeOffsetInHoursDefault);

        public bool EnableVotes => ReadSetting(nameof(EnableVotes), EnableVotesDefault);

        public bool EnableLucene => ReadSetting(nameof(EnableLucene), EnableLuceneDefault);

        public bool EnablePop3 => ReadSetting(nameof(EnablePop3), EnablePop3Default);

        public bool ErrorEmailEnabled => ReadSetting(nameof(ErrorEmailEnabled), ErrorEmailEnabledDefault);

        public string ErrorEmailTo => ReadSetting(nameof(ErrorEmailTo), ErrorEmailToDefault);

        public string ErrorEmailFrom => ReadSetting(nameof(ErrorEmailFrom), ErrorEmailFromDefault);

        public bool MemoryLogEnabled => ReadSetting(nameof(MemoryLogEnabled), MemoryLogEnabledDefault);

        public bool SvnTrustPathsInUrls => ReadSetting(nameof(SvnTrustPathsInUrls), SvnTrustPathsInUrlsDefault);

        public int WhatsNewMaxItemsCount => ReadSetting(nameof(WhatsNewMaxItemsCount), WhatsNewMaxItemsCountDefault);

        public string GitHookUsername => ReadSetting(nameof(GitHookUsername), GitHookUsernameDefault);

        public string GitBugidRegexPattern => ReadSetting(nameof(GitBugidRegexPattern), GitBugidRegexPatternDefault);

        public string SvnHookUsername => ReadSetting(nameof(SvnHookUsername), SvnHookUsernameDefault);

        public string SvnBugidRegexPattern1 => ReadSetting(nameof(SvnBugidRegexPattern1), SvnBugidRegexPattern1Default);

        public string SvnBugidRegexPattern2 => ReadSetting(nameof(SvnBugidRegexPattern2), SvnBugidRegexPattern2Default);

        public string MercurialHookUsername => ReadSetting(nameof(MercurialHookUsername), MercurialHookUsernameDefault);

        public string MercurialBugidRegexPattern => ReadSetting(nameof(MercurialBugidRegexPattern), MercurialBugidRegexPatternDefault);

        public string GitPathToGit => ReadSetting(nameof(GitPathToGit), GitPathToGitDefault);

        public string MercurialPathToHg => ReadSetting(nameof(MercurialPathToHg), MercurialPathToHgDefault);

        public string SubversionPathToSvn => ReadSetting(nameof(SubversionPathToSvn), SubversionPathToSvnDefault);

        public string SubversionAdditionalArgs => ReadSetting(nameof(SubversionAdditionalArgs), SubversionAdditionalArgsDefault);

        public string SQLServerDateFormat => ReadSetting(nameof(SQLServerDateFormat), SQLServerDateFormatDefault);

        public bool NoCapitalization => ReadSetting(nameof(NoCapitalization), NoCapitalizationDefault);

        public bool LimitUsernameDropdownsInSearch => ReadSetting(nameof(LimitUsernameDropdownsInSearch), LimitUsernameDropdownsInSearchDefault);

        public bool RequireStrongPasswords => ReadSetting(nameof(RequireStrongPasswords), RequireStrongPasswordsDefault);

        public bool WriteUtf8Preamble => ReadSetting(nameof(WriteUtf8Preamble), WriteUtf8PreambleDefault);

        public bool ForceBordersInEmails => ReadSetting(nameof(ForceBordersInEmails), ForceBordersInEmailsDefault);

        public string CustomPostLinkLabel => ReadSetting(nameof(CustomPostLinkLabel), CustomPostLinkLabelDefault);

        public string CustomPostLinkUrl => ReadSetting(nameof(CustomPostLinkUrl), CustomPostLinkUrlDefault);

        public bool ShowPotentiallyDangerousHtml => ReadSetting(nameof(ShowPotentiallyDangerousHtml), ShowPotentiallyDangerousHtmlDefault);

        public string CommentSortOrder => ReadSetting(nameof(CommentSortOrder), CommentSortOrderDefault);

        public bool StoreAttachmentsInDatabase => ReadSetting(nameof(StoreAttachmentsInDatabase), StoreAttachmentsInDatabaseDefault);

        public int FailedLoginAttemptsMinutes => ReadSetting(nameof(FailedLoginAttemptsMinutes), FailedLoginAttemptsMinutesDefault);

        public int FailedLoginAttemptsAllowed => ReadSetting(nameof(FailedLoginAttemptsAllowed), FailedLoginAttemptsAllowedDefault);

        public int StatusResultingFromIncomingEmail => ReadSetting(nameof(StatusResultingFromIncomingEmail), StatusResultingFromIncomingEmailDefault);

        public bool AuthenticateUsingLdap => ReadSetting(nameof(AuthenticateUsingLdap), AuthenticateUsingLdapDefault);

        public string LdapUserDistinguishedName => ReadSetting(nameof(LdapUserDistinguishedName), LdapUserDistinguishedNameDefault);

        public string LdapServer => ReadSetting(nameof(LdapServer), LdapServerDefault);

        public string LdapAuthType => ReadSetting(nameof(LdapAuthType), LdapAuthTypeDefault);

        public bool LogSqlEnabled => ReadSetting(nameof(LogSqlEnabled), LogSqlEnabledDefault);

        public bool BodyEncodingUTF8 => ReadSetting(nameof(BodyEncodingUTF8), BodyEncodingUTF8Default);

        public bool SmtpForceReplaceOfBareLineFeeds => ReadSetting(nameof(SmtpForceReplaceOfBareLineFeeds), SmtpForceReplaceOfBareLineFeedsDefault);

        public string SmtpForceSsl => ReadSetting(nameof(SmtpForceSsl), SmtpForceSslDefault);

        public string EmailAddressSeparatorCharacter => ReadSetting(nameof(EmailAddressSeparatorCharacter), EmailAddressSeparatorCharacterDefault);

        public string Pop3Server => ReadSetting(nameof(Pop3Server), Pop3ServerDefault);

        public int Pop3Port => ReadSetting(nameof(Pop3Port), Pop3PortDefault);

        public bool Pop3UseSSL => ReadSetting(nameof(Pop3UseSSL), Pop3UseSSLDefault);

        public string Pop3ServiceUsername => ReadSetting(nameof(Pop3ServiceUsername), Pop3ServiceUsernameDefault);

        public int Pop3TotalErrorsAllowed => ReadSetting(nameof(Pop3TotalErrorsAllowed), Pop3TotalErrorsAllowedDefault);

        public bool Pop3ReadInputStreamCharByChar => ReadSetting(nameof(Pop3ReadInputStreamCharByChar), Pop3ReadInputStreamCharByCharDefault);

        public string Pop3SubjectMustContain => ReadSetting(nameof(Pop3SubjectMustContain), Pop3SubjectMustContainDefault);

        public string Pop3SubjectCannotContain => ReadSetting(nameof(Pop3SubjectCannotContain), Pop3SubjectCannotContainDefault);

        public string Pop3FromMustContain => ReadSetting(nameof(Pop3FromMustContain), Pop3FromMustContainDefault);

        public string Pop3FromCannotContain => ReadSetting(nameof(Pop3FromCannotContain), Pop3FromCannotContainDefault);

        public bool Pop3DeleteMessagesOnServer => ReadSetting(nameof(Pop3DeleteMessagesOnServer), Pop3DeleteMessagesOnServerDefault);

        public bool Pop3WriteRawMessagesToLog => ReadSetting(nameof(Pop3WriteRawMessagesToLog), Pop3WriteRawMessagesToLogDefault);

        public int Pop3FetchIntervalInMinutes => ReadSetting(nameof(Pop3FetchIntervalInMinutes), Pop3FetchIntervalInMinutesDefault);

        public string AutoReplyText => ReadSetting(nameof(AutoReplyText), AutoReplyTextDefault);

        public bool AutoReplyUseHtmlEmailFormat => ReadSetting(nameof(AutoReplyUseHtmlEmailFormat), AutoReplyUseHtmlEmailFormatDefault);

        public string CreateUserFromEmailAddressIfThisUsername => ReadSetting(nameof(CreateUserFromEmailAddressIfThisUsername), CreateUserFromEmailAddressIfThisUsernameDefault);

        public bool UseEmailDomainAsNewOrgNameWhenCreatingNewUser => ReadSetting(nameof(UseEmailDomainAsNewOrgNameWhenCreatingNewUser), UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault);

        public string CreateUsersFromEmailTemplate => ReadSetting(nameof(CreateUsersFromEmailTemplate), CreateUsersFromEmailTemplateDefault);

        public int SqlCommandCommandTimeout => ReadSetting("SqlCommand.CommandTimeout", SqlCommandCommandTimeoutDefault);

        public bool EnableSeen => ReadSetting(nameof(EnableSeen), EnableSeenDefault);

        public string UpdateBugAfterInsertBugAspxSql => ReadSetting(nameof(UpdateBugAfterInsertBugAspxSql), UpdateBugAfterInsertBugAspxSqlDefault);

        public string NotificationSubjectFormat => ReadSetting(nameof(NotificationSubjectFormat), NotificationSubjectFormatDefault);

        public string CustomMenuLinkLabel => ReadSetting(nameof(CustomMenuLinkLabel), CustomMenuLinkLabelDefault);

        public string CustomMenuLinkUrl => ReadSetting(nameof(CustomMenuLinkUrl), CustomMenuLinkUrlDefault);

        public int SearchSuggestMinChars => ReadSetting(nameof(SearchSuggestMinChars), SearchSuggestMinCharsDefault);

        public int WhatsNewPageIntervalInSeconds => ReadSetting(nameof(WhatsNewPageIntervalInSeconds), WhatsNewPageIntervalInSecondsDefault);

        public string DatepickerDateFormat => ReadSetting(nameof(DatepickerDateFormat), DatepickerDateFormatDefault);

        public bool EnableEditWebConfigPage => ReadSetting(nameof(EnableEditWebConfigPage), EnableEditWebConfigPageDefault);

        private TValue ReadSetting<TValue>(string name, TValue defaultValue)
            where TValue : IConvertible
        {
            var value = ConfigurationManager.AppSettings[name];

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
