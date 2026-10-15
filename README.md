# photoHolidayContainer

## Build a local multi-architecture image

The Dockerfile runs the framework-dependent `Release/photoHolidayContainer.dll`
on the .NET 9 ASP.NET runtime. Buildx selects the matching runtime image for
each target architecture; no architecture-specific application publish is
needed.

Start a local registry once:

```sh
docker run -d --restart unless-stopped \
  --name photo-holiday-local-registry \
  -p 127.0.0.1:5000:5000 \
  -v photo-holiday-registry:/var/lib/registry \
  registry:2
```

Create a Buildx builder once. Host networking lets BuildKit push to the local
registry on Linux:

```sh
docker buildx create \
  --name photo-holiday-local \
  --driver docker-container \
  --driver-opt network=host \
  --use --bootstrap
```

Build and store both architectures in that registry:

```sh
docker buildx build \
  --builder photo-holiday-local \
  --platform linux/amd64,linux/arm64 \
  --tag localhost:5000/photo-holiday-container:local \
  --push .
```

Confirm the manifest lists both platforms:

```sh
docker buildx imagetools inspect localhost:5000/photo-holiday-container:local
```

On a machine that can reach the registry, pull and run the tag; Docker selects
the native architecture automatically. These commands use a registry on the
same machine. For another machine, replace `localhost:5000` with the registry
host's reachable name or IP and configure the registry for that Docker host
(prefer HTTPS):

```sh
docker pull localhost:5000/photo-holiday-container:local
docker run --rm -p 8120:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e 'ConnectionStrings__DefaultConnection=Server=YOUR_DB_HOST;Port=3306;Database=travel;User=root;Password=REPLACE_ME;' \
  -v "$PWD/images:/app/images" \
  -e ImageStorage__RootPath=/app/images \
  localhost:5000/photo-holiday-container:local
```

Replace the database placeholders with credentials for a MariaDB server
reachable from the container. Keep the registry container running while you
need the local multi-architecture tag. An OCI archive made with
`--output type=oci` is also multi-architecture, but `docker load` does not load
that archive format.
