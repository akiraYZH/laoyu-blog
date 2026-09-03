# Laoyu Blog Engineering Guidelines

## Communication

- Respond primarily in Chinese unless the user requests another language.
- Lead with the outcome, business reason, and data flow before explaining syntax.
- Ground explanations in the current files, routes, and request flow instead of giving only abstract definitions.
- Work in coherent, verifiable feature increments. Do not fragment a single feature into unnecessarily small steps.
- When answering "why" questions, explain ownership boundaries, cause and effect, and the trade-offs behind the design.
- Clearly distinguish source inspection, static verification, and runtime verification.

## Operational Boundaries

- Confirm the Git root, current implementation, and working-tree status before editing.
- Preserve existing user changes and avoid modifying unrelated files.
- Read-only inspection and task-related formatting, type checking, linting, and builds are allowed.
- Do not run database migrations, database updates, destructive data operations, Docker cleanup, deployment, or other environment-changing commands without explicit confirmation.
- Do not commit or push changes. Let the user review them first.
- Do not use `--force`, `--legacy-peer-deps`, or destructive Git commands to hide underlying problems.

## Backend Architecture

- The backend uses ASP.NET Core Controller Web API, EF Core, and PostgreSQL.
- Controllers own HTTP concerns: bind input, call services, and select status codes. Keep database queries and business workflows out of controllers.
- DTOs define API input/output contracts and request validation. Do not expose EF entities directly through the API.
- Services own business workflows, database queries, and mapping between entities and response DTOs.
- Entities represent persisted domain data. Use `AppDbContext.OnModelCreating` for relationships, indexes, and database constraints.
- Use Action Filters only for genuinely cross-cutting behavior tied to the MVC action lifecycle. Do not use them as a substitute for DTO validation or ordinary business rules.
- Use the global exception handler to translate unhandled exceptions consistently. Introduce specific domain exceptions only when they provide a clear, reusable error boundary.
- Apply filtering and pagination in the database query: `Where`, then `CountAsync`, followed by ordering, `Skip`, and `Take`. Never filter only the current frontend page.
- Create and apply a migration only when the database model changes. Controller logic, query DTOs, service queries, and frontend-only changes do not require migrations.
- Prefer a project-local tool manifest for `dotnet-ef`.

## Frontend Architecture

- The frontend uses Vue 3, TypeScript, Pinia, Vue Router, Ant Design Vue, and Tailwind CSS v4.
- Pinia stores own shared state, cached data, pagination state, and low-level API actions.
- `useBlogPosts` is the component-facing application layer. It calls store actions, centralizes API error handling, and displays feedback through Ant Design `message`.
- A feature component should call the composable that directly belongs to its responsibility. For example, `CategoryFilter` owns category loading and category navigation, while Layout only arranges Header, CategoryFilter, and page content.
- Presentational child components receive data through props and notify parents through semantic emits. Do not pass parent callbacks as props.
- `BlogPostList` owns post presentation, pagination, and edit/delete entry points. It does not own category navigation.
- Reuse `BlogPostForm` for create and update flows. Route-level views load initial data and handle navigation after success.
- Prefer the shared store loading state. Add operation-specific loading state only when concurrent operations must be distinguished.
- Persist category filtering in the home-page URL query, for example `/?category=vue`. Pagination requests must retain the active category.
- Use relative `/api/...` URLs and let Vite or Docker proxy requests to the backend. Do not hard-code container hostnames in components.

## Components and Styling

- Keep Layout structural and free from feature-specific data-fetching logic.
- Apply `class` or `className` to a component's outermost container when the component exposes styling hooks.
- Keep conditional classes explicit and avoid duplicated class logic.
- Tailwind CSS v4 utilities may be overridden by unlayered Ant Design reset styles. Prefer a local, minimal fix. When necessary, use the v4 trailing important modifier, such as `text-white!`, instead of enabling global `important`.
- Verify text contrast on dark backgrounds. Interactive controls need hover, selected, keyboard-focus, and appropriate `aria-*` states.
- Horizontal category navigation must allow scrolling when its content exceeds the viewport.

## Error Handling

- Store actions should throw structured request errors. Composables translate those errors into user-facing feedback.
- Preserve ASP.NET Core validation errors as a field-to-message-array structure. The frontend may display each message through `message.error()`.
- Do not display the same error independently in the store, composable, and component.
- Handle 400, 404, 409, 415, and 500 responses according to their actual meaning rather than converting them into one generic error.

## Verification

- After backend changes, run at least `dotnet build --no-restore`.
- After frontend changes, run targeted Prettier and ESLint checks, `vue-tsc --noEmit`, and a Vite production build.
- Run `git diff --check` and report both the modified files and verification results.
- A successful build proves only static or production-build correctness. Claim runtime verification only after exercising the relevant API request or browser flow.
- Do not delete real posts or modify user data merely to test a feature unless explicitly authorized.

## Technical Documentation

- Each Chinese technical article should solve one clearly defined problem while maintaining continuity with the wider series.
- Document prerequisites, file locations, design intent, implementation steps, verification, and common failure modes so readers can reproduce the project from scratch.
- Do not publish raw chat transcripts as tutorials. Rewrite them as reader-focused, self-contained technical documentation.
