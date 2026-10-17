namespace InteractionSystem
{
    /// <summary>
    /// The analysis texts of one <see cref="InteractData"/> for its Inspector: the state pill and the three "Interactable checks" hints.
    /// Every text is cached against the counts it was made from, so the 200 ms polls allocate only when a count changes. One instance
    /// belongs to one Inspector element (it holds the asset it was made for, never a static).
    /// </summary>
    internal sealed class InteractDataStatus
    {
        private readonly InteractData data;
        private int pillKey = -1;
        private string pillText = string.Empty;
        private int problemsKey = -1;
        private string problemsText;
        private int duplicatesKey = -1;
        private string duplicatesText;
        private int needsKey = -1;
        private string needsText;

        /// <summary>Makes the status of <paramref name="data"/>.</summary>
        /// <param name="data">The asset to analyse; may be null (every text is then empty).</param>
        public InteractDataStatus(InteractData data)
        {
            this.data = data;
        }

        /// <summary>The pill: "Empty" (idle), "N issues" / "N to compile" (warn) or "All compiled" (ok).</summary>
        /// <returns>The text and the pill kind (<c>idle</c>, <c>warn</c>, <c>ok</c>).</returns>
        public (string text, string kind) PillState()
        {
            if (data == null)
            {
                return (string.Empty, "idle");
            }

            int total = data.Interactables.Count;
            if (total == 0)
            {
                return ("Empty", "idle");
            }

            InteractCompiler.Analyze(data, out int needs, out int problems, out int duplicates, out int upToDate);
            int key = (needs * 4096) + (problems * 64) + duplicates + (upToDate << 20);
            if (key != pillKey)
            {
                pillKey = key;
                if (problems + duplicates > 0)
                {
                    pillText = problems + duplicates + (problems + duplicates == 1 ? " issue" : " issues");
                }
                else if (needs > 0)
                {
                    pillText = needs + " to compile";
                }
                else
                {
                    pillText = "All compiled";
                }
            }

            string kind = problems + duplicates > 0 || needs > 0 ? "warn" : "ok";
            return (pillText, kind);
        }

        /// <summary>The hint about entries Compile skips (no prefab, not a prefab asset); null when there are none.</summary>
        public string ProblemsText()
        {
            if (data == null)
            {
                return null;
            }

            InteractCompiler.Analyze(data, out _, out int problems, out _, out _);
            if (problems == 0)
            {
                return null;
            }

            if (problems != problemsKey)
            {
                problemsKey = problems;
                problemsText = problems + (problems == 1 ? " entry has" : " entries have") +
                               " no prefab or an object that is not a prefab asset; Compile skips them.";
            }

            return problemsText;
        }

        /// <summary>The hint about prefabs that are listed more than once; null when there are none.</summary>
        public string DuplicatesText()
        {
            if (data == null)
            {
                return null;
            }

            InteractCompiler.Analyze(data, out _, out _, out int duplicates, out _);
            if (duplicates == 0)
            {
                return null;
            }

            if (duplicates != duplicatesKey)
            {
                duplicatesKey = duplicates;
                duplicatesText = duplicates + (duplicates == 1 ? " entry lists" : " entries list") +
                                 " a prefab that is already listed above; the last one decides what Compile writes.";
            }

            return duplicatesText;
        }

        /// <summary>The hint about prefabs that differ from the list; null when none does.</summary>
        public string NeedsCompileText()
        {
            if (data == null)
            {
                return null;
            }

            InteractCompiler.Analyze(data, out int needs, out _, out _, out _);
            if (needs == 0)
            {
                return null;
            }

            if (needs != needsKey)
            {
                needsKey = needs;
                needsText = needs + (needs == 1 ? " prefab differs" : " prefabs differ") + " from this list. Press Compile to update them.";
            }

            return needsText;
        }
    }
}
