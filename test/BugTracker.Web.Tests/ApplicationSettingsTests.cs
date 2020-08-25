namespace BugTracker.Web.Tests
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class ApplicationSettingsTests
    {
        private readonly Dictionary<string, object> defaultValues = new Dictionary<string, object>
        {
            {nameof(ApplicationSettings.ApplicationTitle), ApplicationSettings.ApplicationTitleDefault},
            {nameof(ApplicationSettings.WindowsAuthentication), ApplicationSettings.WindowsAuthenticationDefault},
            {nameof(ApplicationSettings.AllowGuestWithoutLogin), ApplicationSettings.AllowGuestWithoutLoginDefault},
            {nameof(ApplicationSettings.AllowSelfRegistration), ApplicationSettings.AllowSelfRegistrationDefault},
            {nameof(ApplicationSettings.RegistrationExpiration), ApplicationSettings.RegistrationExpirationDefault},
            {nameof(ApplicationSettings.SelfRegisteredUserTemplate), ApplicationSettings.SelfRegisteredUserTemplateDefault},
            {nameof(ApplicationSettings.ShowForgotPasswordLink), ApplicationSettings.ShowForgotPasswordLinkDefault}
        };

        [Test]
        public void ApplicationSettings_should_return_default_values()
        {
            //Arrange
            var configurationSectionMock = new Mock<IConfigurationSection>();

            configurationSectionMock.Setup(x => x[It.IsAny<string>()])
                .Returns<string>(x => Convert.ToString(defaultValues[x]));

            var configurationMock = new Mock<IConfiguration>();

            configurationMock.Setup(x => x.GetSection(It.IsAny<string>()))
                .Returns(configurationSectionMock.Object);

            var applicationSettings = new ApplicationSettings(configurationMock.Object);

            //Act
            //Assert
            Assert.That(applicationSettings.ApplicationTitle, Is.EqualTo(ApplicationSettings.ApplicationTitleDefault));
            Assert.That(applicationSettings.WindowsAuthentication, Is.EqualTo(ApplicationSettings.WindowsAuthenticationDefault));
            Assert.That(applicationSettings.AllowGuestWithoutLogin, Is.EqualTo(ApplicationSettings.AllowGuestWithoutLoginDefault));
            Assert.That(applicationSettings.AllowSelfRegistration, Is.EqualTo(ApplicationSettings.AllowSelfRegistrationDefault));
            Assert.That(applicationSettings.RegistrationExpiration, Is.EqualTo(ApplicationSettings.RegistrationExpirationDefault));
            Assert.That(applicationSettings.SelfRegisteredUserTemplate, Is.EqualTo(ApplicationSettings.SelfRegisteredUserTemplateDefault));
            Assert.That(applicationSettings.ShowForgotPasswordLink, Is.EqualTo(ApplicationSettings.ShowForgotPasswordLinkDefault));

            //Assert.That(applicationSettings.NotificationEmailFrom, Is.EqualTo(ApplicationSettings.NotificationEmailFromDefault));
            //Assert.That(applicationSettings.AbsoluteUrlPrefix, Is.EqualTo(ApplicationSettings.AbsoluteUrlPrefixDefault));
            //Assert.That(applicationSettings.ConnectionString, Is.EqualTo(ApplicationSettings.ConnectionStringDefault));
            //Assert.That(applicationSettings.EnableWindowsUserAutoRegistration, Is.EqualTo(ApplicationSettings.EnableWindowsUserAutoRegistrationDefault));
            //Assert.That(applicationSettings.WindowsUserAutoRegistrationUserTemplate, Is.EqualTo(ApplicationSettings.WindowsUserAutoRegistrationUserTemplateDefault));
            //Assert.That(applicationSettings.EnableWindowsUserAutoRegistrationLdapSearch, Is.EqualTo(ApplicationSettings.EnableWindowsUserAutoRegistrationLdapSearchDefault));
            //Assert.That(applicationSettings.LdapDirectoryEntryPath, Is.EqualTo(ApplicationSettings.LdapDirectoryEntryPathDefault));
            //Assert.That(applicationSettings.LdapDirectoryEntryAuthenticationType, Is.EqualTo(ApplicationSettings.LdapDirectoryEntryAuthenticationTypeDefault));
            //Assert.That(applicationSettings.LdapDirectoryEntryUsername, Is.EqualTo(ApplicationSettings.LdapDirectoryEntryUsernameDefault));
            //Assert.That(applicationSettings.LdapDirectoryEntryPassword, Is.EqualTo(ApplicationSettings.LdapDirectoryEntryPasswordDefault));
            //Assert.That(applicationSettings.LdapDirectorySearcherFilter, Is.EqualTo(ApplicationSettings.LdapDirectorySearcherFilterDefault));
            //Assert.That(applicationSettings.LdapFirstName, Is.EqualTo(ApplicationSettings.LdapFirstNameDefault));
            //Assert.That(applicationSettings.LdapLastName, Is.EqualTo(ApplicationSettings.LdapLastNameDefault));
            //Assert.That(applicationSettings.LdapEmail, Is.EqualTo(ApplicationSettings.LdapEmailDefault));
            //Assert.That(applicationSettings.LdapEmailSignature, Is.EqualTo(ApplicationSettings.LdapEmailSignatureDefault));
            //Assert.That(applicationSettings.EnableMobile, Is.EqualTo(ApplicationSettings.EnableMobileDefault));
            //Assert.That(applicationSettings.PluralBugLabel, Is.EqualTo(ApplicationSettings.PluralBugLabelDefault));
            //Assert.That(applicationSettings.UseTransmitFileInsteadOfWriteFile, Is.EqualTo(ApplicationSettings.UseTransmitFileInsteadOfWriteFileDefault));
            //Assert.That(applicationSettings.DefaultPermissionLevel, Is.EqualTo(ApplicationSettings.DefaultPermissionLevelDefault));
            //Assert.That(applicationSettings.EnableInternalOnlyPosts, Is.EqualTo(ApplicationSettings.EnableInternalOnlyPostsDefault));
            //Assert.That(applicationSettings.MaxUploadSize, Is.EqualTo(ApplicationSettings.MaxUploadSizeDefault));
            //Assert.That(applicationSettings.SingularBugLabel, Is.EqualTo(ApplicationSettings.SingularBugLabelDefault));
            //Assert.That(applicationSettings.DisplayAnotherButtonInEditBugPage, Is.EqualTo(ApplicationSettings.DisplayAnotherButtonInEditBugPageDefault));
            //Assert.That(applicationSettings.EnableRelationships, Is.EqualTo(ApplicationSettings.EnableRelationshipsDefault));
            //Assert.That(applicationSettings.EnableSubversionIntegration, Is.EqualTo(ApplicationSettings.EnableSubversionIntegrationDefault));
            //Assert.That(applicationSettings.EnableGitIntegration, Is.EqualTo(ApplicationSettings.EnableGitIntegrationDefault));
            //Assert.That(applicationSettings.EnableMercurialIntegration, Is.EqualTo(ApplicationSettings.EnableMercurialIntegrationDefault));
            //Assert.That(applicationSettings.EnableTasks, Is.EqualTo(ApplicationSettings.EnableTasksDefault));
            //Assert.That(applicationSettings.EnableTags, Is.EqualTo(ApplicationSettings.EnableTagsDefault));
            //Assert.That(applicationSettings.CustomBugLinkLabel, Is.EqualTo(ApplicationSettings.CustomBugLinkLabelDefault));
            //Assert.That(applicationSettings.CustomBugLinkUrl, Is.EqualTo(ApplicationSettings.CustomBugLinkUrlDefault));
            //Assert.That(applicationSettings.UseFullNames, Is.EqualTo(ApplicationSettings.UseFullNamesDefault));
            //Assert.That(applicationSettings.ShowUserDefinedBugAttribute, Is.EqualTo(ApplicationSettings.ShowUserDefinedBugAttributeDefault));
            //Assert.That(applicationSettings.TrackBugHistory, Is.EqualTo(ApplicationSettings.TrackBugHistoryDefault));
            //Assert.That(applicationSettings.EnableWhatsNewPage, Is.EqualTo(ApplicationSettings.EnableWhatsNewPageDefault));
            //Assert.That(applicationSettings.NotificationEmailEnabled, Is.EqualTo(ApplicationSettings.NotificationEmailEnabledDefault));
            //Assert.That(applicationSettings.UserDefinedBugAttributeName, Is.EqualTo(ApplicationSettings.UserDefinedBugAttributeNameDefault));
            //Assert.That(applicationSettings.TextAreaThreshold, Is.EqualTo(ApplicationSettings.TextAreaThresholdDefault));
            //Assert.That(applicationSettings.MaxTextAreaRows, Is.EqualTo(ApplicationSettings.MaxTextAreaRowsDefault));
            //Assert.That(applicationSettings.BugLinkMarker, Is.EqualTo(ApplicationSettings.BugLinkMarkerDefault));
            //Assert.That(applicationSettings.DisableFCKEditor, Is.EqualTo(ApplicationSettings.DisableFCKEditorDefault));
            //Assert.That(applicationSettings.ShowTaskAssignedTo, Is.EqualTo(ApplicationSettings.ShowTaskAssignedToDefault));
            //Assert.That(applicationSettings.ShowTaskPlannedStartDate, Is.EqualTo(ApplicationSettings.ShowTaskPlannedStartDateDefault));
            //Assert.That(applicationSettings.ShowTaskActualStartDate, Is.EqualTo(ApplicationSettings.ShowTaskActualStartDateDefault));
            //Assert.That(applicationSettings.ShowTaskPlannedEndDate, Is.EqualTo(ApplicationSettings.ShowTaskPlannedEndDateDefault));
            //Assert.That(applicationSettings.ShowTaskActualEndDate, Is.EqualTo(ApplicationSettings.ShowTaskActualEndDateDefault));
            //Assert.That(applicationSettings.ShowTaskPlannedDuration, Is.EqualTo(ApplicationSettings.ShowTaskPlannedDurationDefault));
            //Assert.That(applicationSettings.ShowTaskActualDuration, Is.EqualTo(ApplicationSettings.ShowTaskActualDurationDefault));
            //Assert.That(applicationSettings.ShowTaskDurationUnits, Is.EqualTo(ApplicationSettings.ShowTaskDurationUnitsDefault));
            //Assert.That(applicationSettings.ShowTaskPercentComplete, Is.EqualTo(ApplicationSettings.ShowTaskPercentCompleteDefault));
            //Assert.That(applicationSettings.ShowTaskStatus, Is.EqualTo(ApplicationSettings.ShowTaskStatusDefault));
            //Assert.That(applicationSettings.ShowTaskSortSequence, Is.EqualTo(ApplicationSettings.ShowTaskSortSequenceDefault));
            //Assert.That(applicationSettings.TaskDefaultDurationUnits, Is.EqualTo(ApplicationSettings.TaskDefaultDurationUnitsDefault));
            //Assert.That(applicationSettings.TaskDefaultHour, Is.EqualTo(ApplicationSettings.TaskDefaultHourDefault));
            //Assert.That(applicationSettings.TaskDefaultStatus, Is.EqualTo(ApplicationSettings.TaskDefaultStatusDefault));
            //Assert.That(applicationSettings.StripDisplayNameFromEmailAddress, Is.EqualTo(ApplicationSettings.StripDisplayNameFromEmailAddressDefault));
            //Assert.That(applicationSettings.TrackingIdString, Is.EqualTo(ApplicationSettings.TrackingIdStringDefault));
            //Assert.That(applicationSettings.SearchSQL, Is.EqualTo(ApplicationSettings.SearchSQLDefault));
            //Assert.That(applicationSettings.AspNetFormId, Is.EqualTo(ApplicationSettings.AspNetFormIdDefault));
            //Assert.That(applicationSettings.LogEnabled, Is.EqualTo(ApplicationSettings.LogEnabledDefault));
            //Assert.That(applicationSettings.StripHtmlTagsFromSearchableText, Is.EqualTo(ApplicationSettings.StripHtmlTagsFromSearchableTextDefault));
            //Assert.That(applicationSettings.JustDateFormat, Is.EqualTo(ApplicationSettings.JustDateFormatDefault));
            //Assert.That(applicationSettings.DateTimeFormat, Is.EqualTo(ApplicationSettings.DateTimeFormatDefault));
            //Assert.That(applicationSettings.DisplayTimeOffsetInHours, Is.EqualTo(ApplicationSettings.DisplayTimeOffsetInHoursDefault));
            //Assert.That(applicationSettings.EnableVotes, Is.EqualTo(ApplicationSettings.EnableVotesDefault));
            //Assert.That(applicationSettings.EnableLucene, Is.EqualTo(ApplicationSettings.EnableLuceneDefault));
            //Assert.That(applicationSettings.ErrorEmailEnabled, Is.EqualTo(ApplicationSettings.ErrorEmailEnabledDefault));
            //Assert.That(applicationSettings.ErrorEmailTo, Is.EqualTo(ApplicationSettings.ErrorEmailToDefault));
            //Assert.That(applicationSettings.ErrorEmailFrom, Is.EqualTo(ApplicationSettings.ErrorEmailFromDefault));
            //Assert.That(applicationSettings.MemoryLogEnabled, Is.EqualTo(ApplicationSettings.MemoryLogEnabledDefault));
            //Assert.That(applicationSettings.SvnTrustPathsInUrls, Is.EqualTo(ApplicationSettings.SvnTrustPathsInUrlsDefault));
            //Assert.That(applicationSettings.WhatsNewMaxItemsCount, Is.EqualTo(ApplicationSettings.WhatsNewMaxItemsCountDefault));
            //Assert.That(applicationSettings.GitHookUsername, Is.EqualTo(ApplicationSettings.GitHookUsernameDefault));
            //Assert.That(applicationSettings.GitBugidRegexPattern, Is.EqualTo(ApplicationSettings.GitBugidRegexPatternDefault));
            //Assert.That(applicationSettings.SvnBugidRegexPattern1, Is.EqualTo(ApplicationSettings.SvnBugidRegexPattern1Default));
            //Assert.That(applicationSettings.SvnBugidRegexPattern2, Is.EqualTo(ApplicationSettings.SvnBugidRegexPattern2Default));
            //Assert.That(applicationSettings.MercurialHookUsername, Is.EqualTo(ApplicationSettings.MercurialHookUsernameDefault));
            //Assert.That(applicationSettings.MercurialBugidRegexPattern, Is.EqualTo(ApplicationSettings.MercurialBugidRegexPatternDefault));
            //Assert.That(applicationSettings.GitPathToGit, Is.EqualTo(ApplicationSettings.GitPathToGitDefault));
            //Assert.That(applicationSettings.MercurialPathToHg, Is.EqualTo(ApplicationSettings.MercurialPathToHgDefault));
            //Assert.That(applicationSettings.SubversionPathToSvn, Is.EqualTo(ApplicationSettings.SubversionPathToSvnDefault));
            //Assert.That(applicationSettings.SubversionAdditionalArgs, Is.EqualTo(ApplicationSettings.SubversionAdditionalArgsDefault));
            //Assert.That(applicationSettings.SQLServerDateFormat, Is.EqualTo(ApplicationSettings.SQLServerDateFormatDefault));
            //Assert.That(applicationSettings.NoCapitalization, Is.EqualTo(ApplicationSettings.NoCapitalizationDefault));
            //Assert.That(applicationSettings.LimitUsernameDropdownsInSearch, Is.EqualTo(ApplicationSettings.LimitUsernameDropdownsInSearchDefault));
            //Assert.That(applicationSettings.RequireStrongPasswords, Is.EqualTo(ApplicationSettings.RequireStrongPasswordsDefault));
            //Assert.That(applicationSettings.WriteUtf8Preamble, Is.EqualTo(ApplicationSettings.WriteUtf8PreambleDefault));
            //Assert.That(applicationSettings.ForceBordersInEmails, Is.EqualTo(ApplicationSettings.ForceBordersInEmailsDefault));
            //Assert.That(applicationSettings.CustomPostLinkLabel, Is.EqualTo(ApplicationSettings.CustomPostLinkLabelDefault));
            //Assert.That(applicationSettings.CustomPostLinkUrl, Is.EqualTo(ApplicationSettings.CustomPostLinkUrlDefault));
            //Assert.That(applicationSettings.ShowPotentiallyDangerousHtml, Is.EqualTo(ApplicationSettings.ShowPotentiallyDangerousHtmlDefault));
            //Assert.That(applicationSettings.CommentSortOrder, Is.EqualTo(ApplicationSettings.CommentSortOrderDefault));
            //Assert.That(applicationSettings.StoreAttachmentsInDatabase, Is.EqualTo(ApplicationSettings.StoreAttachmentsInDatabaseDefault));
            //Assert.That(applicationSettings.FailedLoginAttemptsMinutes, Is.EqualTo(ApplicationSettings.FailedLoginAttemptsMinutesDefault));
            //Assert.That(applicationSettings.FailedLoginAttemptsAllowed, Is.EqualTo(ApplicationSettings.FailedLoginAttemptsAllowedDefault));
            //Assert.That(applicationSettings.StatusResultingFromIncomingEmail, Is.EqualTo(ApplicationSettings.StatusResultingFromIncomingEmailDefault));
            //Assert.That(applicationSettings.AuthenticateUsingLdap, Is.EqualTo(ApplicationSettings.AuthenticateUsingLdapDefault));
            //Assert.That(applicationSettings.LdapUserDistinguishedName, Is.EqualTo(ApplicationSettings.LdapUserDistinguishedNameDefault));
            //Assert.That(applicationSettings.LdapServer, Is.EqualTo(ApplicationSettings.LdapServerDefault));
            //Assert.That(applicationSettings.LdapAuthType, Is.EqualTo(ApplicationSettings.LdapAuthTypeDefault));
            //Assert.That(applicationSettings.LogSqlEnabled, Is.EqualTo(ApplicationSettings.LogSqlEnabledDefault));
            //Assert.That(applicationSettings.SmtpForceReplaceOfBareLineFeeds, Is.EqualTo(ApplicationSettings.SmtpForceReplaceOfBareLineFeedsDefault));
            //Assert.That(applicationSettings.SmtpForceSsl, Is.EqualTo(ApplicationSettings.SmtpForceSslDefault));
            //Assert.That(applicationSettings.EmailAddressSeparatorCharacter, Is.EqualTo(ApplicationSettings.EmailAddressSeparatorCharacterDefault));
            //Assert.That(applicationSettings.Pop3Server, Is.EqualTo(ApplicationSettings.Pop3ServerDefault));
            //Assert.That(applicationSettings.Pop3Port, Is.EqualTo(ApplicationSettings.Pop3PortDefault));
            //Assert.That(applicationSettings.Pop3UseSSL, Is.EqualTo(ApplicationSettings.Pop3UseSSLDefault));
            //Assert.That(applicationSettings.Pop3ServiceUsername, Is.EqualTo(ApplicationSettings.Pop3ServiceUsernameDefault));
            //Assert.That(applicationSettings.Pop3TotalErrorsAllowed, Is.EqualTo(ApplicationSettings.Pop3TotalErrorsAllowedDefault));
            //Assert.That(applicationSettings.Pop3ReadInputStreamCharByChar, Is.EqualTo(ApplicationSettings.Pop3ReadInputStreamCharByCharDefault));
            //Assert.That(applicationSettings.Pop3SubjectMustContain, Is.EqualTo(ApplicationSettings.Pop3SubjectMustContainDefault));
            //Assert.That(applicationSettings.Pop3SubjectCannotContain, Is.EqualTo(ApplicationSettings.Pop3SubjectCannotContainDefault));
            //Assert.That(applicationSettings.Pop3FromMustContain, Is.EqualTo(ApplicationSettings.Pop3FromMustContainDefault));
            //Assert.That(applicationSettings.Pop3FromCannotContain, Is.EqualTo(ApplicationSettings.Pop3FromCannotContainDefault));
            //Assert.That(applicationSettings.Pop3DeleteMessagesOnServer, Is.EqualTo(ApplicationSettings.Pop3DeleteMessagesOnServerDefault));
            //Assert.That(applicationSettings.Pop3WriteRawMessagesToLog, Is.EqualTo(ApplicationSettings.Pop3WriteRawMessagesToLogDefault));
            //Assert.That(applicationSettings.Pop3FetchIntervalInMinutes, Is.EqualTo(ApplicationSettings.Pop3FetchIntervalInMinutesDefault));
            //Assert.That(applicationSettings.AutoReplyText, Is.EqualTo(ApplicationSettings.AutoReplyTextDefault));
            //Assert.That(applicationSettings.AutoReplyUseHtmlEmailFormat, Is.EqualTo(ApplicationSettings.AutoReplyUseHtmlEmailFormatDefault));
            //Assert.That(applicationSettings.CreateUserFromEmailAddressIfThisUsername, Is.EqualTo(ApplicationSettings.CreateUserFromEmailAddressIfThisUsernameDefault));
            //Assert.That(applicationSettings.UseEmailDomainAsNewOrgNameWhenCreatingNewUser, Is.EqualTo(ApplicationSettings.UseEmailDomainAsNewOrgNameWhenCreatingNewUserDefault));
            //Assert.That(applicationSettings.CreateUsersFromEmailTemplate, Is.EqualTo(ApplicationSettings.CreateUsersFromEmailTemplateDefault));
            //Assert.That(applicationSettings.SqlCommandCommandTimeout, Is.EqualTo(ApplicationSettings.SqlCommandCommandTimeoutDefault));
            //Assert.That(applicationSettings.EnableSeen, Is.EqualTo(ApplicationSettings.EnableSeenDefault));
            //Assert.That(applicationSettings.UpdateBugAfterInsertBugAspxSql, Is.EqualTo(ApplicationSettings.UpdateBugAfterInsertBugAspxSqlDefault));
            //Assert.That(applicationSettings.NotificationSubjectFormat, Is.EqualTo(ApplicationSettings.NotificationSubjectFormatDefault));
            //Assert.That(applicationSettings.CustomMenuLinkLabel, Is.EqualTo(ApplicationSettings.CustomMenuLinkLabelDefault));
            //Assert.That(applicationSettings.CustomMenuLinkUrl, Is.EqualTo(ApplicationSettings.CustomMenuLinkUrlDefault));
            //Assert.That(applicationSettings.SearchSuggestMinChars, Is.EqualTo(ApplicationSettings.SearchSuggestMinCharsDefault));
            //Assert.That(applicationSettings.WhatsNewPageIntervalInSeconds, Is.EqualTo(ApplicationSettings.WhatsNewPageIntervalInSecondsDefault));
            //Assert.That(applicationSettings.DatepickerDateFormat, Is.EqualTo(ApplicationSettings.DatepickerDateFormatDefault));
        }
    }
}
