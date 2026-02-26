using Shouldly;

namespace WarLeague.Test
{
    /// <summary>
    /// Team behavior specifications. Covers TeamService in AAA style; no direct DB usage in test bodies.
    /// </summary>
    public partial class Specifications
    {
        #region Team modifications disabled

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCantCreateTeam()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);
            await EnsureConferenceAsync(seasonId, "Default");
            var captain = await CreatePlayer(1001);

            // Act
            var result = await _teamService.CreateAsync(seasonId, "NewTeam", captain.Id, "Default", canBypassTeamModificationCheck: false);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("disabled");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCantDeleteTeam()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(1002);
            await CreateTeam(seasonId, "ToDelete", captain.Id);
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Act
            var result = await _teamService.DeleteAsync(seasonId, "ToDelete");

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("disabled");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCantAddMember()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(1003);
            await CreateTeam(seasonId, "TeamA", captain.Id);
            var newPlayer = await CreatePlayer(1004);
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Act
            var result = await _teamService.AddMemberAsync(seasonId, newPlayer.Id, "TeamA", canBypassTeamModificationCheck: false);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("disabled");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCantRemoveMember()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (playerId, captainId) = await CreateTeamWithPlayer(seasonId, "TeamB");
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Act
            var result = await _teamService.RemoveMemberAsync(seasonId, playerId, canBypassTeamModificationCheck: false);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("disabled");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCantTransferMember()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (playerId, _) = await CreateTeamWithPlayer(seasonId, "Team1");
            var (_, _) = await CreateTeamWithPlayer(seasonId, "Team2");
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Act
            var result = await _teamService.TransferMemberAsync(seasonId, playerId, "Team2", canBypassTeamModificationCheck: false);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("disabled");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCantTransferCaptainship()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (memberId, captainId) = await CreateTeamWithPlayer(seasonId, "TeamC");
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Act
            var result = await _teamService.TransferCaptainshipAsync(seasonId, memberId, "TeamC", canBypassTeamModificationCheck: false);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("disabled");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTeamModificationsDisabled_ThenCanBypassAndCreateTeam()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);
            await EnsureConferenceAsync(seasonId, "Default");
            var captain = await CreatePlayer(1007);

            // Act
            var result = await _teamService.CreateAsync(seasonId, "BypassTeam", captain.Id, "Default", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Create team

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCreatingTeamWithValidParameters_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await EnsureConferenceAsync(seasonId, "Default");
            var captain = await CreatePlayer(2001);

            // Act
            var result = await _teamService.CreateAsync(seasonId, "ValidTeam", captain.Id, "Default", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCreatingTeamWithDuplicateNameInSeason_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(2002);
            await CreateTeam(seasonId, "DupTeam", captain.Id);
            var anotherCaptain = await CreatePlayer(2003);
            await EnsureConferenceAsync(seasonId, "Default");

            // Act
            var result = await _teamService.CreateAsync(seasonId, "DupTeam", anotherCaptain.Id, "Default", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already exists");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCreatingTeamWithEmptyConferenceName_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(2004);

            // Act
            var result = await _teamService.CreateAsync(seasonId, "NoConf", captain.Id, "", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("Conference");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCreatingTeamWithNonExistentConference_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(2005);

            // Act
            var result = await _teamService.CreateAsync(seasonId, "NoConfTeam", captain.Id, "NoSuchConference", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("does not exist");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCreatingTeamWithCaptainAlreadyInAnotherTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(2006);
            await CreateTeam(seasonId, "FirstTeam", captain.Id);
            await EnsureConferenceAsync(seasonId, "Default");

            // Act
            var result = await _teamService.CreateAsync(seasonId, "SecondTeam", captain.Id, "Default", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already a member");
        }

        #endregion

        #region Delete team

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenDeletingExistingTeamWithModificationsEnabled_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(3001);
            await CreateTeam(seasonId, "ToRemove", captain.Id);

            // Act
            var result = await _teamService.DeleteAsync(seasonId, "ToRemove");

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("deleted");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenDeletingNonExistentTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _teamService.DeleteAsync(seasonId, "NoSuchTeam");

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not found");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenDeletingTeamWithExistingMatchups_ThenReturnsFail()
        {
            // Arrange: season with teams and matches (Team1 has matchups vs OpponentTeam)
            var (seasonId, teamName, _, _, _) = await CreateTwoPlayersWithMatchesScenario();

            // Act
            var result = await _teamService.DeleteAsync(seasonId, teamName);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("cannot be deleted");
            result.Message.ShouldContain("matches");
        }

        #endregion

        #region Add member

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenAddingMemberToExistingTeam_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(4001);
            var teamId = await CreateTeam(seasonId, "AddTeam", captain.Id);
            var newPlayer = await CreatePlayer(4002);

            // Act
            var result = await _teamService.AddMemberAsync(seasonId, newPlayer.Id, teamId, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenAddingMemberThatAlreadyInAnotherTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (playerId, _) = await CreateTeamWithPlayer(seasonId, "TeamOne");
            await CreateTeam(seasonId, "TeamTwo", (await CreatePlayer(4004)).Id);

            // Act
            var result = await _teamService.AddMemberAsync(seasonId, playerId, "TeamTwo", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already a member");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenAddingMemberToNonExistentTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var player = await CreatePlayer(4005);

            // Act
            var result = await _teamService.AddMemberAsync(seasonId, player.Id, "NoSuchTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("does not exist");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCaptainAddMemberWithValidCaptain_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(4006);
            await CreateTeam(seasonId, "CapTeam", captain.Id);
            var newPlayer = await CreatePlayer(4007);

            // Act
            var result = await _teamService.CaptainAddMemberAsync(seasonId, captain.Id, newPlayer.Id, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenCaptainAddMemberWhenNotCaptain_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (memberId, captainId) = await CreateTeamWithPlayer(seasonId, "CapTeam2");
            var otherPlayer = await CreatePlayer(4009);

            // Act
            var result = await _teamService.CaptainAddMemberAsync(seasonId, memberId, otherPlayer.Id, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not captain");
        }

        #endregion

        #region Remove member

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenRemovingMemberFromTeam_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (memberId, captainId) = await CreateTeamWithPlayer(seasonId, "RemTeam");

            // Act
            var result = await _teamService.RemoveMemberAsync(seasonId, memberId, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenRemovingCaptain_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(5002);
            await CreateTeam(seasonId, "CapRemTeam", captain.Id);

            // Act
            var result = await _teamService.RemoveMemberAsync(seasonId, captain.Id, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("captain");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenRemovingPlayerNotOnTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await CreateTeamWithPlayer(seasonId, "SomeTeam");
            var outsider = await CreatePlayer(5004);

            // Act
            var result = await _teamService.RemoveMemberAsync(seasonId, outsider.Id, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not a member");
        }

        #endregion

        #region Transfer member

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTransferringMemberToAnotherTeam_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (memberId, _) = await CreateTeamWithPlayer(seasonId, "FromTeam");
            var (_, _) = await CreateTeamWithPlayer(seasonId, "ToTeam");

            // Act
            var result = await _teamService.TransferMemberAsync(seasonId, memberId, "ToTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("transferred");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTransferringCaptain_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (_, captainId) = await CreateTeamWithPlayer(seasonId, "CapTeam");
            await CreateTeamWithPlayer(seasonId, "OtherTeam");

            // Act
            var result = await _teamService.TransferMemberAsync(seasonId, captainId, "OtherTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("Captains");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTransferringToNonExistentTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (memberId, _) = await CreateTeamWithPlayer(seasonId, "RealTeam");

            // Act
            var result = await _teamService.TransferMemberAsync(seasonId, memberId, "NoSuchTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("does not exist");
        }

        #endregion

        #region Transfer captainship

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTransferringCaptainshipToExistingMember_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var (memberId, captainId) = await CreateTeamWithPlayer(seasonId, "XferCapTeam");

            // Act
            var result = await _teamService.TransferCaptainshipAsync(seasonId, memberId, "XferCapTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("transferred");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTransferringCaptainshipToNonMember_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await CreateTeamWithPlayer(seasonId, "OnlyTeam");
            var outsider = await CreatePlayer(6003);

            // Act
            var result = await _teamService.TransferCaptainshipAsync(seasonId, outsider.Id, "OnlyTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not a member");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenTransferringCaptainshipForNonExistentTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var player = await CreatePlayer(6004);

            // Act
            var result = await _teamService.TransferCaptainshipAsync(seasonId, player.Id, "NoSuchTeam", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("does not exist");
        }

        #endregion

        #region Assign Discord role / Update conference

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenAssigningDiscordRoleToExistingTeam_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(7001);
            await CreateTeam(seasonId, "DiscordTeam", captain.Id);

            // Act
            var result = await _teamService.AssignDiscordRoleIdAsync(seasonId, "DiscordTeam", 12345, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenAssigningDiscordRoleToNonExistentTeam_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _teamService.AssignDiscordRoleIdAsync(seasonId, "NoSuchTeam", 12345, canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("does not exist");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenUpdatingConferenceToValidConference_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await EnsureConferenceAsync(seasonId, "Alpha");
            await EnsureConferenceAsync(seasonId, "Beta");
            var captain = await CreatePlayer(7004);
            await CreateTeam(seasonId, "MoveTeam", captain.Id, "Alpha");

            // Act
            var result = await _teamService.UpdateConferenceAsync(seasonId, "MoveTeam", "Beta", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("Beta");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenUpdatingConferenceToNonExistentConference_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(7005);
            await CreateTeam(seasonId, "StayTeam", captain.Id);

            // Act
            var result = await _teamService.UpdateConferenceAsync(seasonId, "StayTeam", "NoSuchConference", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("does not exist");
        }

        [Fact]
        [Trait("Category", "Team")]
        public async Task WhenUpdatingConferenceWithEmptyName_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer(7006);
            await CreateTeam(seasonId, "EmptyConfTeam", captain.Id);

            // Act
            var result = await _teamService.UpdateConferenceAsync(seasonId, "EmptyConfTeam", "", canBypassTeamModificationCheck: true);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("Conference");
        }

        #endregion
    }
}
