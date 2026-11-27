# __PROJECT__ web image: the SvelteKit app with adapter-node.
#
# Build context: the repository root:
#   docker build -f apps/web/Dockerfile .
# The container reads its environment at startup (see deploy/compose.yaml):
#   ORIGIN                      the public origin, for example http://localhost:3000
#   __NAMESPACE___BACKEND_URL   the server-side backend address, for example http://api:8080
# PORT defaults to 3000.

FROM node:24-slim AS build
RUN npm install -g pnpm@11.28.5
WORKDIR /repo
COPY package.json pnpm-workspace.yaml .npmrc ./
COPY apps/web/package.json ./apps/web/package.json
COPY packages/api/package.json ./packages/api/package.json
COPY packages/ui/package.json ./packages/ui/package.json
# The template carries no lockfile, so a fresh checkout resolves within the
# ranges package.json states.
RUN pnpm install --no-frozen-lockfile
COPY apps/web ./apps/web
COPY packages/api ./packages/api
COPY packages/ui ./packages/ui
RUN pnpm --filter @__SCOPE__/ui build
RUN pnpm --filter web build

FROM node:24-slim AS runtime
ENV NODE_ENV=production
ENV PORT=3000
WORKDIR /srv/web
COPY --from=build --chown=node:node /repo/apps/web/build ./build
COPY --from=build --chown=node:node /repo/apps/web/package.json ./package.json
COPY --from=build --chown=node:node /repo/apps/web/node_modules ./node_modules
USER node
EXPOSE 3000
HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD node -e "fetch('http://localhost:' + (process.env.PORT || '3000') + '/').then((r) => { if (!r.ok) process.exit(1); }).catch(() => process.exit(1))"
CMD ["node", "build"]
