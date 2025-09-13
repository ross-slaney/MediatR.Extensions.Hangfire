# NuGet Publishing Setup Guide

This guide walks you through setting up automated NuGet publishing for the MediatR.Hangfire.Extensions package.

## 📋 Prerequisites

- GitHub repository with the project
- GitHub Actions enabled
- .NET 8.0 SDK

## 🔧 Step-by-Step Setup

### 1. Create NuGet Account

1. **Visit** [nuget.org](https://www.nuget.org)
2. **Sign up** for a new account or sign in
3. **Verify** your email address
4. **Complete** your profile information

### 2. Reserve Package Name (Recommended)

1. **Go to** your NuGet profile → "Manage Packages"
2. **Click** "Reserve prefix"
3. **Enter** `MediatR.Hangfire.Extensions`
4. **Submit** the reservation request
5. **Wait** for approval (usually within 24 hours)

### 3. Generate API Key

1. **Navigate to** your NuGet profile → "API Keys"
2. **Click** "Create" to create a new API key
3. **Configure the API key**:
   - **Key Name**: `GitHub-Actions-MediatR-Hangfire-Extensions`
   - **Select Scopes**: ✅ `Push new packages and package versions`
   - **Select Packages**: `*` (or be specific if you have multiple packages)
   - **Glob Pattern**: `MediatR.Hangfire.Extensions*`
   - **Expires**: Choose an appropriate expiration (recommend 1 year)
4. **Click** "Create"
5. **Copy** the generated API key immediately (you won't see it again!)

### 4. Add GitHub Secret

1. **Go to** your GitHub repository
2. **Click** Settings → Secrets and variables → Actions
3. **Click** "New repository secret"
4. **Add the secret**:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste your NuGet API key from step 3
5. **Click** "Add secret"

### 5. Update Project Metadata

Update the following fields in `src/MediatR.Hangfire.Extensions/MediatR.Hangfire.Extensions.csproj`:

```xml
<Authors>Your Name</Authors>
<Company>Your Company</Company>
<PackageProjectUrl>https://github.com/YOUR-USERNAME/MediatR.Hangfire.Extensions</PackageProjectUrl>
<RepositoryUrl>https://github.com/YOUR-USERNAME/MediatR.Hangfire.Extensions</RepositoryUrl>
```

Replace:

- `Your Name` with your actual name
- `Your Company` with your company name (or personal brand)
- `YOUR-USERNAME` with your GitHub username

### 6. Test the Pipeline

1. **Commit** your changes
2. **Push** to a feature branch
3. **Create** a pull request to `main`
4. **Verify** that CI tests pass
5. **Merge** the PR to trigger NuGet publishing

## 🚀 How It Works

### Automatic Versioning

The CI pipeline uses **GitVersion** for semantic versioning:

- **Main branch**: Production releases (1.0.0, 1.0.1, 1.1.0)
- **Develop branch**: Alpha releases (1.1.0-alpha.1)
- **Feature branches**: Alpha releases (1.1.0-alpha.2)
- **Pull requests**: Pull request builds (1.1.0-PullRequest.123)

### Publishing Triggers

NuGet packages are published when:

- ✅ **Main branch** receives a push (direct or via PR merge)
- ✅ **All tests pass**
- ✅ **Code quality checks pass**
- ✅ **Security scans pass**

### Package Contents

Each NuGet package includes:

- ✅ **Compiled library** (.dll)
- ✅ **Debug symbols** (.pdb)
- ✅ **XML documentation** (.xml)
- ✅ **README.md** (displayed on NuGet.org)
- ✅ **LICENSE** file
- ✅ **Package icon** (if present)

## 🛠️ Manual Publishing (Backup)

If you need to publish manually:

```bash
# Build and pack
dotnet pack src/MediatR.Hangfire.Extensions/MediatR.Hangfire.Extensions.csproj \
  --configuration Release \
  --output ./packages \
  -p:PackageVersion=1.0.0

# Publish to NuGet
dotnet nuget push ./packages/*.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

## 🔍 Monitoring

### Check Publishing Status

1. **GitHub Actions**: Monitor the "Package & Publish" job
2. **NuGet.org**: Check your packages page for new versions
3. **Logs**: Review CI logs for any publishing errors

### Package Metrics

Track your package success on NuGet.org:

- Download counts
- Version adoption
- User feedback

## 🚨 Troubleshooting

### Common Issues

**❌ "Package already exists"**

- Update version number in GitVersion or manually
- Use `--skip-duplicate` flag (already included in CI)

**❌ "Invalid API key"**

- Regenerate API key on NuGet.org
- Update GitHub secret with new key

**❌ "Package validation failed"**

- Check package metadata in .csproj
- Ensure all required fields are filled
- Verify LICENSE file exists

**❌ "Build failed"**

- Ensure all tests pass locally
- Check for compilation errors
- Verify dependencies are available

### Support

- **NuGet Issues**: Contact NuGet support
- **GitHub Actions**: Check Actions documentation
- **GitVersion**: Review GitVersion documentation

## 🎉 Success!

Once setup is complete, your package will be automatically published to NuGet.org whenever you merge to the main branch. Users can install it with:

```bash
dotnet add package MediatR.Hangfire.Extensions
```

Your package will be discoverable and installable by the entire .NET community! 🚀
