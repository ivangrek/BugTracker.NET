namespace BugTracker.Web.Core
{
    using System;
    using System.Configuration;
    using Identification;
    using Microsoft.Extensions.Configuration;

    public interface IApplicationSettings
    {
        //string this[string index] { get; }

        string ApplicationTitle { get; }

        AuthenticationMode WindowsAuthentication { get; }

        //bool AllowGuestWithoutLogin { get; }

        bool AllowSelfRegistration { get; }

        //bool ShowForgotPasswordLink { get; }



        //int RegistrationExpiration { get; }

        //string SelfRegisteredUserTemplate { get; }

        //string NotificationEmailFrom { get; }

        //string AbsoluteUrlPrefix { get; }

        //string ConnectionString { get; }

        //bool EnableWindowsUserAutoRegistration { get; }

        //string WindowsUserAutoRegistrationUserTemplate { get; }

        //bool EnableWindowsUserAutoRegistrationLdapSearch { get; }

        //string LdapDirectoryEntryPath { get; }

        //string LdapDirectoryEntryAuthenticationType { get; }

        //string LdapDirectoryEntryUsername { get; }

        //string LdapDirectoryEntryPassword { get; }

        //string LdapDirectorySearcherFilter { get; }

        //string LdapFirstName { get; }

        //string LdapLastName { get; }

        //string LdapEmail { get; }

        //string LdapEmailSignature { get; }

        //bool EnableMobile { get; }

        //string PluralBugLabel { get; }

        //bool UseTransmitFileInsteadOfWriteFile { get; }

        //int DefaultPermissionLevel { get; }

        //bool EnableInternalOnlyPosts { get; }

        //int MaxUploadSize { get; }

        //string SingularBugLabel { get; }

        //bool DisplayAnotherButtonInEditBugPage { get; }

        //bool EnableRelationships { get; }

        //bool EnableSubversionIntegration { get; }

        //bool EnableGitIntegration { get; }

        //bool EnableMercurialIntegration { get; }

        //bool EnableTasks { get; }

        //bool EnableTags { get; }

        //string CustomBugLinkLabel { get; }

        //string CustomBugLinkUrl { get; }

        //bool UseFullNames { get; }

        //bool ShowUserDefinedBugAttribute { get; }

        //bool TrackBugHistory { get; }

        //bool EnableWhatsNewPage { get; }

        //bool NotificationEmailEnabled { get; }

        //string UserDefinedBugAttributeName { get; }

        //int TextAreaThreshold { get; }

        //int MaxTextAreaRows { get; }

        //string BugLinkMarker { get; }

        //bool DisableFCKEditor { get; }

        //bool ShowTaskAssignedTo { get; }

        //bool ShowTaskPlannedStartDate { get; }

        //bool ShowTaskActualStartDate { get; }

        //bool ShowTaskPlannedEndDate { get; }

        //bool ShowTaskActualEndDate { get; }

        //bool ShowTaskPlannedDuration { get; }

        //bool ShowTaskActualDuration { get; }

        //bool ShowTaskDurationUnits { get; }

        //bool ShowTaskPercentComplete { get; }

        //bool ShowTaskStatus { get; }

        //bool ShowTaskSortSequence { get; }

        //string TaskDefaultDurationUnits { get; }

        //string TaskDefaultHour { get; }

        //string TaskDefaultStatus { get; }

        //bool StripDisplayNameFromEmailAddress { get; }

        //string TrackingIdString { get; }

        //string SearchSQL { get; }

        //string AspNetFormId { get; }

        //bool LogEnabled { get; }

        //bool StripHtmlTagsFromSearchableText { get; }

        //string JustDateFormat { get; }

        //string DateTimeFormat { get; }

        //int DisplayTimeOffsetInHours { get; }

        //bool EnableVotes { get; }

        //bool EnableLucene { get; }

        //bool ErrorEmailEnabled { get; }

        //string ErrorEmailTo { get; }

        //string ErrorEmailFrom { get; }

        //bool MemoryLogEnabled { get; }

        //bool SvnTrustPathsInUrls { get; }

        //int WhatsNewMaxItemsCount { get; }

        //string GitHookUsername { get; }

        //string GitBugidRegexPattern { get; }

        //string SvnHookUsername { get; }

        //string SvnBugidRegexPattern1 { get; }

        //string SvnBugidRegexPattern2 { get; }

        //string MercurialHookUsername { get; }

        //string MercurialBugidRegexPattern { get; }

        //string GitPathToGit { get; }

        //string MercurialPathToHg { get; }

        //string SubversionPathToSvn { get; }

        //string SubversionAdditionalArgs { get; }

        //string SQLServerDateFormat { get; }

        //bool NoCapitalization { get; }

        //bool LimitUsernameDropdownsInSearch { get; }

        //bool RequireStrongPasswords { get; }

        //bool WriteUtf8Preamble { get; }

        //bool ForceBordersInEmails { get; }

        //string CustomPostLinkLabel { get; }

        //string CustomPostLinkUrl { get; }

        //bool ShowPotentiallyDangerousHtml { get; }

        //string CommentSortOrder { get; }

        //bool StoreAttachmentsInDatabase { get; }

        //int FailedLoginAttemptsMinutes { get; }

        //int FailedLoginAttemptsAllowed { get; }

        //int StatusResultingFromIncomingEmail { get; }

        //bool AuthenticateUsingLdap { get; }

        //string LdapUserDistinguishedName { get; }

        //string LdapServer { get; }

        //string LdapAuthType { get; }

        //bool LogSqlEnabled { get; }

        //bool BodyEncodingUTF8 { get; }

        //bool SmtpForceReplaceOfBareLineFeeds { get; }

        //string SmtpForceSsl { get; }

        //string EmailAddressSeparatorCharacter { get; }

        //string Pop3Server { get; }

        //int Pop3Port { get; }

        //bool Pop3UseSSL { get; }

        //string Pop3ServiceUsername { get; }

        //int Pop3TotalErrorsAllowed { get; }

        //bool Pop3ReadInputStreamCharByChar { get; }

        //string Pop3SubjectMustContain { get; }

        //string Pop3SubjectCannotContain { get; }

        //string Pop3FromMustContain { get; }

        //string Pop3FromCannotContain { get; }

        //bool Pop3DeleteMessagesOnServer { get; }

        //bool Pop3WriteRawMessagesToLog { get; }

        //int Pop3FetchIntervalInMinutes { get; }

        //string AutoReplyText { get; }

        //bool AutoReplyUseHtmlEmailFormat { get; }

        //string CreateUserFromEmailAddressIfThisUsername { get; }

        //bool UseEmailDomainAsNewOrgNameWhenCreatingNewUser { get; }

        //string CreateUsersFromEmailTemplate { get; }

        //int SqlCommandCommandTimeout { get; }

        //bool EnableSeen { get; }

        //string UpdateBugAfterInsertBugAspxSql { get; }

        //string NotificationSubjectFormat { get; }

        //string CustomMenuLinkLabel { get; }

        //string CustomMenuLinkUrl { get; }

        //int SearchSuggestMinChars { get; }

        //int WhatsNewPageIntervalInSeconds { get; }

        //string DatepickerDateFormat { get; }

        //bool EnableEditWebConfigPage { get; }
    }

    internal sealed class ApplicationSettings : IApplicationSettings
    {
        public const string ApplicationTitleDefault = "BugTracker.NET";
        public const AuthenticationMode WindowsAuthenticationDefault = AuthenticationMode.Site;

        private readonly IConfiguration configuration;

        public ApplicationSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }


        //public const bool AllowGuestWithoutLoginDefault = false;
        public const bool AllowSelfRegistrationDefault = false;
        //public const bool ShowForgotPasswordLinkDefault = false;

        //
        //public const int RegistrationExpirationDefault = 20;
        //public const string SelfRegisteredUserTemplateDefault = "[error - missing user template]";
        //public const string NotificationEmailFromDefault = "";
        //public const string AbsoluteUrlPrefixDefault = "";
        //public const string ConnectionStringDefault = " ?";
        //public const bool EnableWindowsUserAutoRegistrationDefault = true;
        //public const string WindowsUserAutoRegistrationUserTemplateDefault = "guest";
        //public const bool EnableWindowsUserAutoRegistrationLdapSearchDefault = false;
        //public const string LdapDirectoryEntryPathDefault = "LDAP://127.0.0.1/DC=mycompany,DC=com";
        //public const string LdapDirectoryEntryAuthenticationTypeDefault = "Anonymous";
        //public const string LdapDirectoryEntryUsernameDefault = "";
        //public const string LdapDirectoryEntryPasswordDefault = "";
        //public const string LdapDirectorySearcherFilterDefault = "(uid=$REPLACE_WITH_USERNAME$)";
        //public const string LdapFirstNameDefault = "gn";
        //public const string LdapLastNameDefault = "sn";
        //public const string LdapEmailDefault = "mail";
        //public const string LdapEmailSignatureDefault = "cn";
        //public const bool EnableMobileDefault = false;
        //public const string SingularBugLabelDefault = "bug";
        //public const string PluralBugLabelDefault = "bugs";
        //public const bool UseTransmitFileInsteadOfWriteFileDefault = false;
        //public const int DefaultPermissionLevelDefault = 2;
        //public const bool EnableInternalOnlyPostsDefault = false;
        //public const int MaxUploadSizeDefault = 100000;
        //public const bool DisplayAnotherButtonInEditBugPageDefault = false;
        //public const bool EnableRelationshipsDefault = false;
        //public const bool EnableSubversionIntegrationDefault = false;
        //public const bool EnableGitIntegrationDefault = false;
        //public const bool EnableMercurialIntegrationDefault = false;
        //public const bool EnableTasksDefault = false;
        //public const bool EnableTagsDefault = false;
        //public const string CustomBugLinkLabelDefault = "";
        //public const string CustomBugLinkUrlDefault = "";
        //public const bool UseFullNamesDefault = false;
        //public const bool ShowUserDefinedBugAttributeDefault = true;
        //public const bool TrackBugHistoryDefault = true;
        //public const bool EnableWhatsNewPageDefault = false;
        //public const bool NotificationEmailEnabledDefault = true;
        //public const string UserDefinedBugAttributeNameDefault = "YOUR ATTRIBUTE";
        //public const int TextAreaThresholdDefault = 100;
        //public const int MaxTextAreaRowsDefault = 5;
        //public const string BugLinkMarkerDefault = "bugid#";
        //public const bool DisableFCKEditorDefault = false;
        //public const bool ShowTaskAssignedToDefault = true;
        //public const bool ShowTaskPlannedStartDateDefault = true;
        //public const bool ShowTaskActualStartDateDefault = true;
        //public const bool ShowTaskPlannedEndDateDefault = true;
        //public const bool ShowTaskActualEndDateDefault = true;
        //public const bool ShowTaskPlannedDurationDefault = true;
        //public const bool ShowTaskActualDurationDefault = true;
        //public const bool ShowTaskDurationUnitsDefault = true;
        //public const bool ShowTaskPercentCompleteDefault = true;
        //public const bool ShowTaskStatusDefault = true;
        //public const bool ShowTaskSortSequenceDefault = true;
        //public const string TaskDefaultDurationUnitsDefault = "hours";
        //public const string TaskDefaultHourDefault = "09";
        //public const string TaskDefaultStatusDefault = "[no status]";
        //public const bool StripDisplayNameFromEmailAddressDefault = false;
        //public const string TrackingIdStringDefault = "DO NOT EDIT THIS:";
        //public const string SearchSQLDefault = "";
        //public const string AspNetFormIdDefault = "ctl00";
        //public const bool LogEnabledDefault = true;
        //public const bool StripHtmlTagsFromSearchableTextDefault = true;
        //public const string JustDateFormatDefault = "g";
        //public const string DateTimeFormatDefault = "g";
        //public const int DisplayTimeOffsetInHoursDefault = 0;
        //public const bool EnableVotesDefault = false;
        //public const bool EnableLuceneDefault = true;
        //public const bool ErrorEmailEnabledDefault = true;
        //public const string ErrorEmailToDefault = "";
        //public const string ErrorEmailFromDefault = "";
        //public const bool MemoryLogEnabledDefault = true;
        //public const bool SvnTrustPathsInUrlsDefault = false;
        //public const int WhatsNewMaxItemsCountDefault = 200;
        //public const string GitHookUsernameDefault = "";
        //public const string GitBugidRegexPatternDefault = "(^[0-9]+)";
        //public const string SvnHookUsernameDefault = "";
        //public const string SvnBugidRegexPattern1Default = "([0-9,]+$)";
        //public const string SvnBugidRegexPattern2Default = "(^[0-9,]+ )";
        //public const string MercurialHookUsernameDefault = "";
        //public const string MercurialBugidRegexPatternDefault = "(^[0-9]+)";
        //public const string GitPathToGitDefault = "[path to git.exe?]";
        //public const string MercurialPathToHgDefault = "[path to hg.exe?]";
        //public const string SubversionPathToSvnDefault = "svn";
        //public const string SubversionAdditionalArgsDefault = "";
        //public const string SQLServerDateFormatDefault = "yyyyMMdd HH:mm:ss";
        //public const bool NoCapitalizationDefault = false;
        //public const bool LimitUsernameDropdownsInSearchDefault = false;
        //public const bool RequireStrongPasswordsDefault = false;
        //public const bool WriteUtf8PreambleDefault = true;
        //public const bool ForceBordersInEmailsDefault = false;
        //public const string CustomPostLinkLabelDefault = "";
        //public const string CustomPostLinkUrlDefault = "";
        //public const bool ShowPotentiallyDangerousHtmlDefault = false;
        //public const string CommentSortOrderDefault = "desc";
        //public const bool StoreAttachmentsInDatabaseDefault = false;
        //public const int FailedLoginAttemptsMinutesDefault = 10;
        //public const int FailedLoginAttemptsAllowedDefault = 10;
        //public const int StatusResultingFromIncomingEmailDefault = 0;
        //public const bool AuthenticateUsingLdapDefault = false;
        //public const string LdapUserDistinguishedNameDefault = "uid=$REPLACE_WITH_USERNAME$,ou=people,dc=mycompany,dc=com";
        //public const string LdapServerDefault = "127.0.0.1";
        //public const string LdapAuthTypeDefault = "Basic";
        //public const bool LogSqlEnabledDefault = true;
        //public const bool BodyEncodingUTF8Default = true;
        //public const bool SmtpForceReplaceOfBareLineFeedsDefault = false;
        //public const string SmtpForceSslDefault = "";
        //public const string EmailAddressSeparatorCharacterDefault = ",";
        //public const string Pop3ServerDefault = "pop.gmail.com";
        //public const int Pop3PortDefault = 995;
        //public const bool Pop3UseSSLDefault = true;
        //public const string Pop3ServiceUsernameDefault = "admin";
        //public const int Pop3TotalErrorsAllowedDefault = 100;
        //public const bool Pop3ReadInputStreamCharByCharDefault = false;
        //public const string Pop3SubjectMustContainDefault = "";
        //public const string Pop3SubjectCannotContainDefault = "";
        //public const string Pop3FromMustContainDefault = "";
        //public const string Pop3FromCannotContainDefault = "";
        //public const bool Pop3DeleteMessagesOnServerDefault = false;
        //public const bool Pop3WriteRawMessagesToLogDefault = false;
        //public const int Pop3FetchIntervalInMinutesDefault = 15;
        //public const string AutoReplyTextDefault = "";
        //public const bool AutoReplyUseHtmlEmailFormatDefault = false;
        //public const string CreateUserFromEmailAddressIfThisUsernameDefault = "";
        //public const bool UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault = false;
        //public const string CreateUsersFromEmailTemplateDefault = "[error - missing user template]";
        //public const int SqlCommandCommandTimeoutDefault = 30;
        //public const bool EnableSeenDefault = false;
        //public const string UpdateBugAfterInsertBugAspxSqlDefault = "";
        //public const string NotificationSubjectFormatDefault = "$THING$:$BUGID$ was $ACTION$ - $SHORTDESC$ $TRACKINGID$";
        //public const string CustomMenuLinkLabelDefault = "";
        //public const string CustomMenuLinkUrlDefault = "";
        //public const int SearchSuggestMinCharsDefault = 3;
        //public const int WhatsNewPageIntervalInSecondsDefault = 20;
        //public const string DatepickerDateFormatDefault = "yy-mm-dd";
        //public const bool EnableEditWebConfigPageDefault = false;

        //public string this[string index] => ReadOption(index, string.Empty);

        public string ApplicationTitle => ReadOption(nameof(ApplicationTitle), ApplicationTitleDefault);

        public AuthenticationMode WindowsAuthentication => ReadOption(nameof(WindowsAuthentication), WindowsAuthenticationDefault);

        //public bool AllowGuestWithoutLogin => ReadOption(nameof(AllowGuestWithoutLogin), AllowGuestWithoutLoginDefault);

        public bool AllowSelfRegistration => ReadOption(nameof(AllowSelfRegistration), AllowSelfRegistrationDefault);

        //public bool ShowForgotPasswordLink => ReadOption(nameof(ShowForgotPasswordLink), ShowForgotPasswordLinkDefault);



        //public int RegistrationExpiration => ReadOption(nameof(RegistrationExpiration), RegistrationExpirationDefault);

        //public string SelfRegisteredUserTemplate => ReadOption(nameof(SelfRegisteredUserTemplate), SelfRegisteredUserTemplateDefault);

        //public string NotificationEmailFrom => ReadOption(nameof(NotificationEmailFrom), NotificationEmailFromDefault);

        //public string AbsoluteUrlPrefix => ReadOption(nameof(AbsoluteUrlPrefix), AbsoluteUrlPrefixDefault);

        //public string ConnectionString => ReadOption(nameof(ConnectionString), ConnectionStringDefault);

        //public bool EnableWindowsUserAutoRegistration => ReadOption(nameof(EnableWindowsUserAutoRegistration), EnableWindowsUserAutoRegistrationDefault);

        //public string WindowsUserAutoRegistrationUserTemplate => ReadOption(nameof(WindowsUserAutoRegistrationUserTemplate), WindowsUserAutoRegistrationUserTemplateDefault);

        //public bool EnableWindowsUserAutoRegistrationLdapSearch => ReadOption(nameof(EnableWindowsUserAutoRegistrationLdapSearch), EnableWindowsUserAutoRegistrationLdapSearchDefault);

        //public string LdapDirectoryEntryPath => ReadOption(nameof(LdapDirectoryEntryPath), LdapDirectoryEntryPathDefault);

        //public string LdapDirectoryEntryAuthenticationType => ReadOption(nameof(LdapDirectoryEntryAuthenticationType), LdapDirectoryEntryAuthenticationTypeDefault);

        //public string LdapDirectoryEntryUsername => ReadOption(nameof(LdapDirectoryEntryUsername), LdapDirectoryEntryUsernameDefault);

        //public string LdapDirectoryEntryPassword => ReadOption(nameof(LdapDirectoryEntryPassword), LdapDirectoryEntryPasswordDefault);

        //public string LdapDirectorySearcherFilter => ReadOption(nameof(LdapDirectorySearcherFilter), LdapDirectorySearcherFilterDefault);

        //public string LdapFirstName => ReadOption(nameof(LdapFirstName), LdapFirstNameDefault);

        //public string LdapLastName => ReadOption(nameof(LdapLastName), LdapLastNameDefault);

        //public string LdapEmail => ReadOption(nameof(LdapEmail), LdapEmailDefault);

        //public string LdapEmailSignature => ReadOption(nameof(LdapEmailSignature), LdapEmailSignatureDefault);

        //public bool EnableMobile => ReadOption(nameof(EnableMobile), EnableMobileDefault);

        //public string SingularBugLabel => ReadOption(nameof(SingularBugLabel), SingularBugLabelDefault);

        //public string PluralBugLabel => ReadOption(nameof(PluralBugLabel), PluralBugLabelDefault);

        //public bool UseTransmitFileInsteadOfWriteFile => ReadOption(nameof(UseTransmitFileInsteadOfWriteFile), UseTransmitFileInsteadOfWriteFileDefault);

        //public int DefaultPermissionLevel => ReadOption(nameof(DefaultPermissionLevel), DefaultPermissionLevelDefault);

        //public bool EnableInternalOnlyPosts => ReadOption(nameof(EnableInternalOnlyPosts), EnableInternalOnlyPostsDefault);

        //public int MaxUploadSize => ReadOption(nameof(MaxUploadSize), MaxUploadSizeDefault);

        //public bool DisplayAnotherButtonInEditBugPage => ReadOption(nameof(DisplayAnotherButtonInEditBugPage), DisplayAnotherButtonInEditBugPageDefault);

        //public bool EnableRelationships => ReadOption(nameof(EnableRelationships), EnableRelationshipsDefault);

        //public bool EnableSubversionIntegration => ReadOption(nameof(EnableSubversionIntegration), EnableSubversionIntegrationDefault);

        //public bool EnableGitIntegration => ReadOption(nameof(EnableGitIntegration), EnableGitIntegrationDefault);

        //public bool EnableMercurialIntegration => ReadOption(nameof(EnableMercurialIntegration), EnableMercurialIntegrationDefault);

        //public bool EnableTasks => ReadOption(nameof(EnableTasks), EnableTasksDefault);

        //public bool EnableTags => ReadOption(nameof(EnableTags), EnableTagsDefault);

        //public string CustomBugLinkLabel => ReadOption(nameof(CustomBugLinkLabel), CustomBugLinkLabelDefault);

        //public string CustomBugLinkUrl => ReadOption(nameof(CustomBugLinkUrl), CustomBugLinkUrlDefault);

        //public bool UseFullNames => ReadOption(nameof(UseFullNames), UseFullNamesDefault);

        //public bool ShowUserDefinedBugAttribute => ReadOption(nameof(ShowUserDefinedBugAttribute), ShowUserDefinedBugAttributeDefault);

        //public bool TrackBugHistory => ReadOption(nameof(TrackBugHistory), TrackBugHistoryDefault);

        //public bool EnableWhatsNewPage => ReadOption(nameof(EnableWhatsNewPage), EnableWhatsNewPageDefault);

        //public bool NotificationEmailEnabled => ReadOption(nameof(NotificationEmailEnabled), NotificationEmailEnabledDefault);

        //public string UserDefinedBugAttributeName => ReadOption(nameof(UserDefinedBugAttributeName), UserDefinedBugAttributeNameDefault);

        //public int TextAreaThreshold => ReadOption(nameof(TextAreaThreshold), TextAreaThresholdDefault);

        //public int MaxTextAreaRows => ReadOption(nameof(MaxTextAreaRows), MaxTextAreaRowsDefault);

        //public string BugLinkMarker => ReadOption(nameof(BugLinkMarker), BugLinkMarkerDefault);

        //public bool DisableFCKEditor => ReadOption(nameof(DisableFCKEditor), DisableFCKEditorDefault);

        //public bool ShowTaskAssignedTo => ReadOption(nameof(ShowTaskAssignedTo), ShowTaskAssignedToDefault);

        //public bool ShowTaskPlannedStartDate => ReadOption(nameof(ShowTaskPlannedStartDate), ShowTaskPlannedStartDateDefault);

        //public bool ShowTaskActualStartDate => ReadOption(nameof(ShowTaskActualStartDate), ShowTaskActualStartDateDefault);

        //public bool ShowTaskPlannedEndDate => ReadOption(nameof(ShowTaskPlannedEndDate), ShowTaskPlannedEndDateDefault);

        //public bool ShowTaskActualEndDate => ReadOption(nameof(ShowTaskActualEndDate), ShowTaskActualEndDateDefault);

        //public bool ShowTaskPlannedDuration => ReadOption(nameof(ShowTaskPlannedDuration), ShowTaskPlannedDurationDefault);

        //public bool ShowTaskActualDuration => ReadOption(nameof(ShowTaskActualDuration), ShowTaskActualDurationDefault);

        //public bool ShowTaskDurationUnits => ReadOption(nameof(ShowTaskDurationUnits), ShowTaskDurationUnitsDefault);

        //public bool ShowTaskPercentComplete => ReadOption(nameof(ShowTaskPercentComplete), ShowTaskPercentCompleteDefault);

        //public bool ShowTaskStatus => ReadOption(nameof(ShowTaskStatus), ShowTaskStatusDefault);

        //public bool ShowTaskSortSequence => ReadOption(nameof(ShowTaskSortSequence), ShowTaskSortSequenceDefault);

        //public string TaskDefaultDurationUnits => ReadOption(nameof(TaskDefaultDurationUnits), TaskDefaultDurationUnitsDefault);

        //public string TaskDefaultHour => ReadOption(nameof(TaskDefaultHour), TaskDefaultHourDefault);

        //public string TaskDefaultStatus => ReadOption(nameof(TaskDefaultStatus), TaskDefaultStatusDefault);

        //public bool StripDisplayNameFromEmailAddress => ReadOption(nameof(StripDisplayNameFromEmailAddress), StripDisplayNameFromEmailAddressDefault);

        //public string TrackingIdString => ReadOption(nameof(TrackingIdString), TrackingIdStringDefault);

        //public string SearchSQL => ReadOption(nameof(SearchSQL), SearchSQLDefault);

        //public string AspNetFormId => ReadOption(nameof(AspNetFormId), AspNetFormIdDefault);

        //public bool LogEnabled => ReadOption(nameof(LogEnabled), LogEnabledDefault);

        //public bool StripHtmlTagsFromSearchableText => ReadOption(nameof(StripHtmlTagsFromSearchableText), StripHtmlTagsFromSearchableTextDefault);

        //public string JustDateFormat => ReadOption(nameof(JustDateFormat), JustDateFormatDefault);

        //public string DateTimeFormat => ReadOption(nameof(DateTimeFormat), DateTimeFormatDefault);

        //public int DisplayTimeOffsetInHours => ReadOption(nameof(DisplayTimeOffsetInHours), DisplayTimeOffsetInHoursDefault);

        //public bool EnableVotes => ReadOption(nameof(EnableVotes), EnableVotesDefault);

        //public bool EnableLucene => ReadOption(nameof(EnableLucene), EnableLuceneDefault);

        //public bool ErrorEmailEnabled => ReadOption(nameof(ErrorEmailEnabled), ErrorEmailEnabledDefault);

        //public string ErrorEmailTo => ReadOption(nameof(ErrorEmailTo), ErrorEmailToDefault);

        //public string ErrorEmailFrom => ReadOption(nameof(ErrorEmailFrom), ErrorEmailFromDefault);

        //public bool MemoryLogEnabled => ReadOption(nameof(MemoryLogEnabled), MemoryLogEnabledDefault);

        //public bool SvnTrustPathsInUrls => ReadOption(nameof(SvnTrustPathsInUrls), SvnTrustPathsInUrlsDefault);

        //public int WhatsNewMaxItemsCount => ReadOption(nameof(WhatsNewMaxItemsCount), WhatsNewMaxItemsCountDefault);

        //public string GitHookUsername => ReadOption(nameof(GitHookUsername), GitHookUsernameDefault);

        //public string GitBugidRegexPattern => ReadOption(nameof(GitBugidRegexPattern), GitBugidRegexPatternDefault);

        //public string SvnHookUsername => ReadOption(nameof(SvnHookUsername), SvnHookUsernameDefault);

        //public string SvnBugidRegexPattern1 => ReadOption(nameof(SvnBugidRegexPattern1), SvnBugidRegexPattern1Default);

        //public string SvnBugidRegexPattern2 => ReadOption(nameof(SvnBugidRegexPattern2), SvnBugidRegexPattern2Default);

        //public string MercurialHookUsername => ReadOption(nameof(MercurialHookUsername), MercurialHookUsernameDefault);

        //public string MercurialBugidRegexPattern => ReadOption(nameof(MercurialBugidRegexPattern), MercurialBugidRegexPatternDefault);

        //public string GitPathToGit => ReadOption(nameof(GitPathToGit), GitPathToGitDefault);

        //public string MercurialPathToHg => ReadOption(nameof(MercurialPathToHg), MercurialPathToHgDefault);

        //public string SubversionPathToSvn => ReadOption(nameof(SubversionPathToSvn), SubversionPathToSvnDefault);

        //public string SubversionAdditionalArgs => ReadOption(nameof(SubversionAdditionalArgs), SubversionAdditionalArgsDefault);

        //public string SQLServerDateFormat => ReadOption(nameof(SQLServerDateFormat), SQLServerDateFormatDefault);

        //public bool NoCapitalization => ReadOption(nameof(NoCapitalization), NoCapitalizationDefault);

        //public bool LimitUsernameDropdownsInSearch => ReadOption(nameof(LimitUsernameDropdownsInSearch), LimitUsernameDropdownsInSearchDefault);

        //public bool RequireStrongPasswords => ReadOption(nameof(RequireStrongPasswords), RequireStrongPasswordsDefault);

        //public bool WriteUtf8Preamble => ReadOption(nameof(WriteUtf8Preamble), WriteUtf8PreambleDefault);

        //public bool ForceBordersInEmails => ReadOption(nameof(ForceBordersInEmails), ForceBordersInEmailsDefault);

        //public string CustomPostLinkLabel => ReadOption(nameof(CustomPostLinkLabel), CustomPostLinkLabelDefault);

        //public string CustomPostLinkUrl => ReadOption(nameof(CustomPostLinkUrl), CustomPostLinkUrlDefault);

        //public bool ShowPotentiallyDangerousHtml => ReadOption(nameof(ShowPotentiallyDangerousHtml), ShowPotentiallyDangerousHtmlDefault);

        //public string CommentSortOrder => ReadOption(nameof(CommentSortOrder), CommentSortOrderDefault);

        //public bool StoreAttachmentsInDatabase => ReadOption(nameof(StoreAttachmentsInDatabase), StoreAttachmentsInDatabaseDefault);

        //public int FailedLoginAttemptsMinutes => ReadOption(nameof(FailedLoginAttemptsMinutes), FailedLoginAttemptsMinutesDefault);

        //public int FailedLoginAttemptsAllowed => ReadOption(nameof(FailedLoginAttemptsAllowed), FailedLoginAttemptsAllowedDefault);

        //public int StatusResultingFromIncomingEmail => ReadOption(nameof(StatusResultingFromIncomingEmail), StatusResultingFromIncomingEmailDefault);

        //public bool AuthenticateUsingLdap => ReadOption(nameof(AuthenticateUsingLdap), AuthenticateUsingLdapDefault);

        //public string LdapUserDistinguishedName => ReadOption(nameof(LdapUserDistinguishedName), LdapUserDistinguishedNameDefault);

        //public string LdapServer => ReadOption(nameof(LdapServer), LdapServerDefault);

        //public string LdapAuthType => ReadOption(nameof(LdapAuthType), LdapAuthTypeDefault);

        //public bool LogSqlEnabled => ReadOption(nameof(LogSqlEnabled), LogSqlEnabledDefault);

        //public bool BodyEncodingUTF8 => ReadOption(nameof(BodyEncodingUTF8), BodyEncodingUTF8Default);

        //public bool SmtpForceReplaceOfBareLineFeeds => ReadOption(nameof(SmtpForceReplaceOfBareLineFeeds), SmtpForceReplaceOfBareLineFeedsDefault);

        //public string SmtpForceSsl => ReadOption(nameof(SmtpForceSsl), SmtpForceSslDefault);

        //public string EmailAddressSeparatorCharacter => ReadOption(nameof(EmailAddressSeparatorCharacter), EmailAddressSeparatorCharacterDefault);

        //public string Pop3Server => ReadOption(nameof(Pop3Server), Pop3ServerDefault);

        //public int Pop3Port => ReadOption(nameof(Pop3Port), Pop3PortDefault);

        //public bool Pop3UseSSL => ReadOption(nameof(Pop3UseSSL), Pop3UseSSLDefault);

        //public string Pop3ServiceUsername => ReadOption(nameof(Pop3ServiceUsername), Pop3ServiceUsernameDefault);

        //public int Pop3TotalErrorsAllowed => ReadOption(nameof(Pop3TotalErrorsAllowed), Pop3TotalErrorsAllowedDefault);

        //public bool Pop3ReadInputStreamCharByChar => ReadOption(nameof(Pop3ReadInputStreamCharByChar), Pop3ReadInputStreamCharByCharDefault);

        //public string Pop3SubjectMustContain => ReadOption(nameof(Pop3SubjectMustContain), Pop3SubjectMustContainDefault);

        //public string Pop3SubjectCannotContain => ReadOption(nameof(Pop3SubjectCannotContain), Pop3SubjectCannotContainDefault);

        //public string Pop3FromMustContain => ReadOption(nameof(Pop3FromMustContain), Pop3FromMustContainDefault);

        //public string Pop3FromCannotContain => ReadOption(nameof(Pop3FromCannotContain), Pop3FromCannotContainDefault);

        //public bool Pop3DeleteMessagesOnServer => ReadOption(nameof(Pop3DeleteMessagesOnServer), Pop3DeleteMessagesOnServerDefault);

        //public bool Pop3WriteRawMessagesToLog => ReadOption(nameof(Pop3WriteRawMessagesToLog), Pop3WriteRawMessagesToLogDefault);

        //public int Pop3FetchIntervalInMinutes => ReadOption(nameof(Pop3FetchIntervalInMinutes), Pop3FetchIntervalInMinutesDefault);

        //public string AutoReplyText => ReadOption(nameof(AutoReplyText), AutoReplyTextDefault);

        //public bool AutoReplyUseHtmlEmailFormat => ReadOption(nameof(AutoReplyUseHtmlEmailFormat), AutoReplyUseHtmlEmailFormatDefault);

        //public string CreateUserFromEmailAddressIfThisUsername => ReadOption(nameof(CreateUserFromEmailAddressIfThisUsername), CreateUserFromEmailAddressIfThisUsernameDefault);

        //public bool UseEmailDomainAsNewOrgNameWhenCreatingNewUser => ReadOption(nameof(UseEmailDomainAsNewOrgNameWhenCreatingNewUser), UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault);

        //public string CreateUsersFromEmailTemplate => ReadOption(nameof(CreateUsersFromEmailTemplate), CreateUsersFromEmailTemplateDefault);

        //public int SqlCommandCommandTimeout => ReadOption("SqlCommand.CommandTimeout", SqlCommandCommandTimeoutDefault);

        //public bool EnableSeen => ReadOption(nameof(EnableSeen), EnableSeenDefault);

        //public string UpdateBugAfterInsertBugAspxSql => ReadOption(nameof(UpdateBugAfterInsertBugAspxSql), UpdateBugAfterInsertBugAspxSqlDefault);

        //public string NotificationSubjectFormat => ReadOption(nameof(NotificationSubjectFormat), NotificationSubjectFormatDefault);

        //public string CustomMenuLinkLabel => ReadOption(nameof(CustomMenuLinkLabel), CustomMenuLinkLabelDefault);

        //public string CustomMenuLinkUrl => ReadOption(nameof(CustomMenuLinkUrl), CustomMenuLinkUrlDefault);

        //public int SearchSuggestMinChars => ReadOption(nameof(SearchSuggestMinChars), SearchSuggestMinCharsDefault);

        //public int WhatsNewPageIntervalInSeconds => ReadOption(nameof(WhatsNewPageIntervalInSeconds), WhatsNewPageIntervalInSecondsDefault);

        //public string DatepickerDateFormat => ReadOption(nameof(DatepickerDateFormat), DatepickerDateFormatDefault);

        //public bool EnableEditWebConfigPage => ReadOption(nameof(EnableEditWebConfigPage), EnableEditWebConfigPageDefault);

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
