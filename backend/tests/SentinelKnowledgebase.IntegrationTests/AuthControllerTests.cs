using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using AwesomeAssertions;

using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;

using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthControllerTests
{
    private readonly IntegrationTestFixture _fixture;

    public AuthControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithValidBootstrapAdminCredentials_ShouldReturnCurrentUser()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = IntegrationTestFixture.BootstrapAdminEmail,
            Password = IntegrationTestFixture.BootstrapAdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(IntegrationTestFixture.BootstrapAdminEmail);
        user.Role.Should().Be("admin");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = IntegrationTestFixture.BootstrapAdminEmail,
            Password = "invalid-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvitationAcceptanceFlow_ShouldCreateMemberUser()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();

        var invitationResponse = await adminClient.PostAsJsonAsync("/api/auth/invitations", new InvitationRequestDto
        {
            Email = "member@sentinel.test",
            DisplayName = "Member User",
            Role = "member"
        });

        invitationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await invitationResponse.Content.ReadFromJsonAsync<InvitationResponseDto>();
        invitation.Should().NotBeNull();

        var acceptResponse = await adminClient.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequestDto
        {
            Token = invitation!.Token,
            DisplayName = "Member User",
            Password = "Member1234!"
        });

        acceptResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await acceptResponse.Content.ReadFromJsonAsync<AuthUserDto>();
        user.Should().NotBeNull();
        user!.Role.Should().Be("member");
        user.Email.Should().Be("member@sentinel.test");
    }

    [Fact]
    public async Task ApprovedDeviceFlow_ShouldIssueAccessAndRefreshTokens()
    {
        using var anonymousClient = _fixture.CreateClient();
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();

        var startResponse = await anonymousClient.PostAsJsonAsync("/api/auth/device/start", new DeviceStartRequestDto
        {
            DeviceName = "Integration Extension"
        });

        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var startPayload = await startResponse.Content.ReadFromJsonAsync<DeviceStartResponseDto>();
        startPayload.Should().NotBeNull();

        var approveResponse = await adminClient.PostAsJsonAsync("/api/auth/device/approve", new DeviceApproveRequestDto
        {
            UserCode = startPayload!.UserCode
        });

        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var pollResponse = await anonymousClient.PostAsJsonAsync("/api/auth/device/poll", new DevicePollRequestDto
        {
            DeviceCode = startPayload.DeviceCode
        });

        pollResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResponse = await pollResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.User.Role.Should().Be("admin");
    }

    [Fact]
    public async Task ApprovedDeviceFlow_CaptureShouldBeOwnedByApprovingUser()
    {
        using var deviceClient = _fixture.CreateClient();
        var member = await _fixture.CreateMemberClientAsync();
        using var approvingClient = member.Client;
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        var startResponse = await deviceClient.PostAsJsonAsync("/api/auth/device/start", new DeviceStartRequestDto
        {
            DeviceName = "Ownership Verification Extension"
        });

        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var startPayload = await startResponse.Content.ReadFromJsonAsync<DeviceStartResponseDto>();
        startPayload.Should().NotBeNull();

        var approveResponse = await approvingClient.PostAsJsonAsync("/api/auth/device/approve", new DeviceApproveRequestDto
        {
            UserCode = startPayload!.UserCode
        });

        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var pollResponse = await deviceClient.PostAsJsonAsync("/api/auth/device/poll", new DevicePollRequestDto
        {
            DeviceCode = startPayload.DeviceCode
        });

        pollResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResponse = await pollResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.User.Id.Should().Be(memberUserId);

        using var bearerClient = _fixture.CreateClient();
        bearerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        var captureUrl = $"https://example.com/device-owned/{Guid.NewGuid():N}";
        var captureResponse = await bearerClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = captureUrl,
            ContentType = ContentType.Article,
            RawContent = "Device login ownership capture.",
            Tags = new List<string> { "device-login" }
        });

        captureResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await captureResponse.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();

        RawCapture? persistedCapture = null;
        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            persistedCapture = await dbContext.RawCaptures.FindAsync(accepted!.Id);
        });

        persistedCapture.Should().NotBeNull();
        persistedCapture!.OwnerUserId.Should().Be(memberUserId);
        persistedCapture.SourceUrl.Should().Be(captureUrl);
    }
}
