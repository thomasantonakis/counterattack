# Bundled game rulebook

Source: https://github.com/counterattackgame/wiki
Pinned upstream revision: `e9c0f9fe0d575d61368f20cfd412bc9194ca2e71`.
English source pages and eight diagrams were retrieved on 2026-11-07.

`English/` is the editable game edition. It retains upstream rules pending the
chapter audit, with Advanced variants taking precedence. Website language and
home navigation are omitted by the parser. The landing page is adapted for the
viewer. Reckless-tackle sections and dependent references are excluded.
Tabletop setup and contradictory rules are retained for review rather than silently
changed. Source attribution is retained.

Use **CounterAttack > Rules > Rebuild Viewer and Scene Entries** in Edit Mode
after editing Markdown. Save open scenes first. The builder validates internal
links and diagrams, then authors `Assets/Prefabs/RulesViewer.prefab` and places
instances in MainMenu and Room. It does not download content or update the source.
Article objects, contents buttons and search-result slots all exist before Play.

Supported Markdown: headings, paragraphs, ordered and unordered lists, bold,
italic, inline code, inline links and standalone PNG diagrams. The current wiki
does not use tables. Extend the builder before introducing tables or HTML.

The viewer uses a bundled snapshot without runtime network requests. External
links only open in the browser when clicked. Navigation/search state is retained
in memory for this application session.

While searching, Enter advances to the next match and Escape clears the query.
With no query, Escape closes the viewer. Back/Forward track links, contents jumps,
search destinations and manually scrolled reading positions.
