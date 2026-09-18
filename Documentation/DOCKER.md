# Docker Documentation

## 1. Purpose

This document describes how Docker is used in **KUKULCAN.SharedKernel.i18n**.

The project uses Docker for three distinct purposes:

1. Building and running the API locally.
2. Verifying the production container runtime in continuous integration.
3. Publishing production images to Docker Hub from version tags.

The CI smoke test and the Docker Hub publication workflow are intentionally separate. The CI workflow validates that the container can be built and started successfully. The publication workflow is responsible for publishing a production image.

---

## 2. Docker Architecture

The repository contains a root-level `Dockerfile` that uses a multi-stage build.

### Build stage

The build stage uses:

```text
mcr.microsoft.com/dotnet/sdk:10.0
```

It:

- Sets `/src` as the working directory.
- Copies the solution and project files required for restore.
- Restores the API project.
- Copies the complete `Source/` directory.
- Publishes the API in `Release` configuration.
- Writes the published application to `/app/publish`.

The publish command is equivalent to:

```bash
dotnet publish Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj \
  --configuration Release \
  --no-restore \
  --output /app/publish \
  -p:UseAppHost=false \
  -p:GeneratePackageOnBuild=false
```

### Runtime stage

The runtime stage uses:

```text
mcr.microsoft.com/dotnet/aspnet:10.0
```

Only the published application is copied from the build stage:

```text
/app/publish -> /app
```

The container:

- Exposes port `8080`.
- Uses `/app` as its working directory.
- Starts the API with:

```text
dotnet KUKULCAN.SharedKernel.i18n.dll
```

Using a separate runtime image keeps the final image independent of the .NET SDK used during compilation.

---

## 3. Building the Docker Image Locally

From the repository root:

```bash
docker build --tag kukulcan-sharedkernel-i18n:local .
```

Verify that the image exists:

```bash
docker image ls kukulcan-sharedkernel-i18n
```

Inspect the image:

```bash
docker image inspect kukulcan-sharedkernel-i18n:local
```

The Docker image is managed by the Docker Engine; it is not represented by a single ordinary file in the repository.

---

## 4. Running the API Locally

The API listens on port `8080` inside the container.

A basic container can be started with:

```bash
docker run --detach \
  --name kukulcan-sharedkernel-i18n \
  --publish 8080:8080 \
  --env ASPNETCORE_HTTP_PORTS=8080 \
  kukulcan-sharedkernel-i18n:local
```

The option:

```text
--publish 8080:8080
```

maps host port `8080` to container port `8080`.

Check the container:

```bash
docker ps
```

View its logs:

```bash
docker logs kukulcan-sharedkernel-i18n
```

Stop it:

```bash
docker stop kukulcan-sharedkernel-i18n
```

Remove it:

```bash
docker rm kukulcan-sharedkernel-i18n
```

---

## 5. Configuration and Environment Variables

Container configuration is supplied through environment variables.

The CI smoke test uses:

```text
ASPNETCORE_HTTP_PORTS=8080
KUKULCAN__Database__ConnectionString=Host=localhost; Port=5432; Database=SmokeTest; Username=postgres; Password=postgres
Jwt__SecretKey=KUKULCAN.SharedKernel.i18n.Docker.SmokeTest.SecretKey.2026
```

The double underscore in:

```text
KUKULCAN__Database__ConnectionString
```

represents nested .NET configuration sections.

For local development, do not copy CI credentials into production environments. Supply environment-specific values when starting the container.

Example:

```bash
docker run --detach \
  --name kukulcan-sharedkernel-i18n \
  --publish 8080:8080 \
  --env ASPNETCORE_HTTP_PORTS=8080 \
  --env KUKULCAN__Database__ConnectionString='Host=<database-host>; Port=5432; Database=<database>; Username=<username>; Password=<password>' \
  --env Jwt__SecretKey='<application-secret>' \
  kukulcan-sharedkernel-i18n:local
```

Secrets must not be committed to the repository or embedded in the Dockerfile.

---

## 6. Health Check

The CI workflow verifies the running container through:

```text
GET /health/live
```

The smoke test is performed against:

```text
http://127.0.0.1:8080/health/live
```

A successful response means that the container has started sufficiently for the configured liveness endpoint to respond.

Example:

```bash
curl --fail http://127.0.0.1:8080/health/live
```

---

## 7. CI Docker Smoke Test

The existing `.github/workflows/ci.yml` workflow performs the following Docker sequence after the build and tests:

1. Verify that Docker is available on the GitHub runner.
2. Build the image.
3. Start the container.
4. Wait for the liveness endpoint.
5. Collect container logs if the job exits.
6. Remove the container.

The CI image is deliberately tagged:

```text
kukulcan-sharedkernel-i18n:ci
```

It is a validation image only. The CI workflow does **not** publish this image to Docker Hub.

The relevant build command is:

```bash
docker build --tag kukulcan-sharedkernel-i18n:ci .
```

The container is started with:

```bash
docker run --detach \
  --name kukulcan-sharedkernel-i18n-ci \
  --publish 8080:8080 \
  --env ASPNETCORE_HTTP_PORTS=8080 \
  --env KUKULCAN__Database__ConnectionString='Host=localhost; Port=5432; Database=SmokeTest; Username=postgres; Password=postgres' \
  --env Jwt__SecretKey='KUKULCAN.SharedKernel.i18n.Docker.SmokeTest.SecretKey.2026' \
  kukulcan-sharedkernel-i18n:ci
```

The workflow retries the health endpoint for up to 30 attempts, with a two-second delay between attempts.

---

## 8. Production Docker Image Publication

Production publication is implemented separately in:

```text
.github/workflows/docker-publish.yml
```

This workflow is triggered only when a semantic version tag matching:

```text
v*.*.*
```

is pushed.

For example:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The repository currently defines version `1.0.0` in `Directory.Build.props`, so `v1.0.0` is the corresponding initial release-tag example.

The publication workflow:

1. Checks out the tagged source.
2. Configures Docker Buildx.
3. Logs in to Docker Hub.
4. Generates Docker image metadata.
5. Builds the existing root `Dockerfile`.
6. Pushes the production image to Docker Hub.

The image repository name is constructed as:

```text
<DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n
```

The workflow does not hard-code the Docker Hub username.

---

## 9. Docker Hub Secrets

Before the first production publication, the repository must contain these GitHub Actions secrets:

| Secret | Purpose |
|---|---|
| `DOCKERHUB_USERNAME` | Docker Hub account or namespace used for the image repository |
| `DOCKERHUB_TOKEN` | Docker Hub access token used by GitHub Actions to authenticate |

The workflow references them as:

```yaml
username: ${{ secrets.DOCKERHUB_USERNAME }}
password: ${{ secrets.DOCKERHUB_TOKEN }}
```

The token should be a Docker Hub access token suitable for pushing images. Do not put the token in:

- `Dockerfile`
- source code
- workflow YAML
- documentation
- Git history

The actual secret values are intentionally not documented here.

---

## 10. Published Image Tags

When the workflow is triggered by:

```text
v1.0.0
```

the Docker metadata configuration publishes these tags:

```text
<DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:1.0.0
<DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:1.0
<DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:1
<DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:latest
```

Therefore:

- `1.0.0` identifies the exact semantic version.
- `1.0` identifies the major/minor release line.
- `1` identifies the major release line.
- `latest` identifies the current published release.

The source tag contains the leading `v`, while the Docker image version tags do not.

---

## 11. Complete Release Example

The following is an example of publishing version `1.0.0).

### Step 1: Ensure the source version is correct

Check:

```text
Directory.Build.props
```

The current version is:

```xml
<VersionPrefix>1.0.0</VersionPrefix>
<AssemblyVersion>1.0.0</AssemblyVersion>
<FileVersion>1.0.0</FileVersion>
```

### Step 2: Validate the project

Run the normal CI-equivalent validation locally as appropriate:

```bash
dotnet restore KUKULCAN.SharedKernel.i18n.slnx
dotnet build KUKULCAN.SharedKernel.i18n.slnx --configuration Release
```

Run the tests:

```bash
dotnet test --configuration Release
```

Build the container locally:

```bash
docker build --tag kukulcan-sharedkernel-i18n:local .
```

Run it and verify:

```bash
docker run --detach \
  --name kukulcan-sharedkernel-i18n-test \
  --publish 8080:8080 \
  --env ASPNETCORE_HTTP_PORTS=8080 \
  kukulcan-sharedkernel-i18n:local
```

Then:

```bash
curl --fail http://127.0.0.1:8080/health/live
```

Clean up:

```bash
docker rm --force kukulcan-sharedkernel-i18n-test
```

### Step 3: Create the release tag

After the intended commit has been validated:

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Step 4: GitHub Actions publishes the image

The tag push starts:

```text
Publish Docker image
```

The workflow authenticates to Docker Hub using the two repository secrets and publishes the generated image tags.

---

## 12. Pulling and Running a Published Image

After publication, the image can be downloaded from Docker Hub with:

```bash
docker pull <DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:1.0.0
```

Run the exact version:

```bash
docker run --detach \
  --name kukulcan-sharedkernel-i18n \
  --publish 8080:8080 \
  --env ASPNETCORE_HTTP_PORTS=8080 \
  --env KUKULCAN__Database__ConnectionString='<connection-string>' \
  --env Jwt__SecretKey='<application-secret>' \
  <DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:1.0.0
```

For reproducible deployments, prefer an explicit version tag such as `1.0.0` rather than relying on `latest`.

---

## 13. Docker Image Inspection and Maintenance

List local images:

```bash
docker image ls
```

Inspect an image:

```bash
docker image inspect <DOCKERHUB_USERNAME>/kukulcan-sharedkernel-i18n:1.0.0
```

List running containers:

```bash
docker ps
```

List all containers:

```bash
docker ps --all
```

Remove a stopped container:

```bash
docker rm <container-name>
```

Remove a local image:

```bash
docker image rm <image-name>:<tag>
```

Inspect the Docker Engine storage root:

```bash
docker info --format '{{.DockerRootDir}}'
```

Docker manages image layers and metadata internally. The repository itself contains the Dockerfile and workflows, not a generated Docker image file.

---

## 14. Troubleshooting

### Docker daemon is unavailable

Check:

```bash
docker info
```

If the command cannot connect to the Docker daemon, resolve the Docker Engine/service problem before attempting a build.

### The container starts but the health check fails

Inspect the logs:

```bash
docker logs kukulcan-sharedkernel-i18n
```

Check that port `8080` is published:

```bash
docker port kukulcan-sharedkernel-i18n
```

Check the endpoint from the host:

```bash
curl --fail http://127.0.0.1:8080/health/live
```

### Docker build fails during restore

Verify that the project files expected by the Dockerfile still exist under:

```text
Source/
```

The Dockerfile explicitly copies the API, Application, Domain, Infrastructure, and database migration project files before running restore.

### Docker Hub publication fails during login

Verify that both GitHub repository secrets exist:

```text
DOCKERHUB_USERNAME
DOCKERHUB_TOKEN
```

Also verify that the Docker Hub token has permission to push to the target repository.

### The image is built but not published

The CI workflow is not a publication workflow. A normal branch push or pull request runs the CI Docker smoke test but does not push an image.

Production publication requires a pushed tag matching:

```text
v*.*.*
```

---

## 15. Separation Between CI and Production Publication

The project intentionally separates validation from publication.

### CI

File:

```text
.github/workflows/ci.yml
```

Responsibilities:

- Restore.
- Build.
- Run unit tests.
- Run integration tests.
- Build the Docker image.
- Start the container.
- Verify `/health/live`.
- Clean up the test container.

CI image:

```text
kukulcan-sharedkernel-i18n:ci
```

The CI image is not published.

### Production publication

File:

```text
.github/workflows/docker-publish.yml
```

Responsibilities:

- React to semantic version tags.
- Authenticate to Docker Hub.
- Build the production image using the repository Dockerfile.
- Publish versioned and release-alias tags.

This separation keeps Docker runtime validation available on ordinary CI runs without publishing every branch or pull-request build.

---

## 16. Dockerfile Change Guidelines

The Dockerfile is part of the production build contract.

When modifying it:

1. Keep the build and runtime stages explicit.
2. Keep the runtime image based on the ASP.NET 10 image.
3. Keep the API published in `Release` configuration.
4. Keep the application under `/app` in the runtime stage.
5. Keep port `8080` consistent with the application and CI smoke test.
6. Build the image locally after changes.
7. Run the container and verify `/health/live`.
8. Allow the CI workflow to validate the Docker image before a production release tag is used.

Do not add secrets to the Dockerfile.

---

## 17. Files Involved

| File | Responsibility |
|---|---|
| `Dockerfile` | Multi-stage .NET 10 production container definition |
| `.github/workflows/ci.yml` | Build, test, Docker build, container startup, and health smoke test |
| `.github/workflows/docker-publish.yml` | Production Docker Hub publication |
| `Directory.Build.props` | Project version used as the basis for release versioning |

---

## 18. Current Production Flow

The intended production Docker flow is:

```text
Source code
    |
    v
CI build + unit tests + integration tests
    |
    v
Docker build
    |
    v
Container startup + /health/live
    |
    v
Validated commit
    |
    v
Semantic version tag (for example v1.0.0)
    |
    v
docker-publish.yml
    |
    v
Docker Hub authentication
    |
    v
Production Docker build
    |
    v
Docker Hub
    |
    +--> 1.0.0
    +--> 1.0
    +--> 1
    +--> latest
```

This is the Docker lifecycle currently defined for **KUKULCAN.SharedKernel.i18n**.
