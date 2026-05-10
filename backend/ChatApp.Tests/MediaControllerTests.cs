using System.Security.Claims;
using ChatApp.API.Controllers;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ChatApp.Tests;

public class MediaControllerTests
{
    [Fact]
    public async Task InitUpload_RejectsDangerousMimeTypes()
    {
        var controller = CreateController(Guid.NewGuid());
        var request = new UploadInitRequest("payload.exe", "application/x-msdownload", 128, "file");

        var act = async () => await controller.InitUpload(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid content type.");
    }

    [Fact]
    public async Task InitUpload_CreatesMediaRecordAndPresignedUploadUrl()
    {
        var userId = Guid.NewGuid();
        MediaFile? capturedMedia = null;

        var mediaRepository = new Mock<IMediaFileRepository>();
        mediaRepository
            .Setup(repo => repo.AddAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .Callback<MediaFile, CancellationToken>((media, _) => capturedMedia = media)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(uow => uow.MediaFiles).Returns(mediaRepository.Object);
        unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var storage = new Mock<IFileStorageService>();
        storage
            .Setup(s => s.GetPresignedUploadUrlAsync("chat-media", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example/upload");

        var controller = CreateController(userId, unitOfWork.Object, storage.Object);
        var request = new UploadInitRequest("holiday.png", "image/png", 1024, "image");

        var response = await controller.InitUpload(request, CancellationToken.None);

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = ok.Value.Should().BeOfType<ApiResponse<UploadInitResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.UploadUrl.Should().Be("https://storage.example/upload");
        capturedMedia.Should().NotBeNull();
        capturedMedia!.UploadedById.Should().Be(userId);
        capturedMedia.OriginalName.Should().Be("holiday.png");
        capturedMedia.BucketName.Should().Be("chat-media");
        capturedMedia.MimeType.Should().Be("image/png");
    }

    [Fact]
    public async Task CompleteUpload_RejectsMismatchedObjectName()
    {
        var userId = Guid.NewGuid();
        var media = MediaFile.Create(
            userId,
            "image.png",
            "2026/05/10/object-image.png",
            "chat-media",
            "chat-media/2026/05/10/object-image.png",
            "image/png",
            1024);

        var mediaRepository = new Mock<IMediaFileRepository>();
        mediaRepository
            .Setup(repo => repo.GetByIdAsync(media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(uow => uow.MediaFiles).Returns(mediaRepository.Object);

        var controller = CreateController(userId, unitOfWork.Object);
        var request = new UploadCompleteRequest(media.Id, "different-object-name");

        var act = async () => await controller.CompleteUpload(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Upload object name does not match the initialized upload.");
    }

    private static MediaController CreateController(
        Guid userId,
        IUnitOfWork? unitOfWork = null,
        IFileStorageService? storage = null,
        IBackgroundJobClient? backgroundJobs = null)
    {
        var controller = new MediaController(
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            storage ?? Mock.Of<IFileStorageService>(),
            backgroundJobs ?? Mock.Of<IBackgroundJobClient>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }, "test"))
            }
        };

        return controller;
    }
}
