using Godot;
using GridironGM.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GridironGM.Persistence
{
    public sealed class GmProfileStore
    {
        private const string ProfilePath = "user://gm_profiles_v1.json";
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public IReadOnlyList<GmProfile> LoadAll()
        {
            var path = ProjectSettings.GlobalizePath(ProfilePath);
            try
            {
                if (!File.Exists(path))
                    return Array.Empty<GmProfile>();
                return JsonSerializer.Deserialize<List<GmProfile>>(File.ReadAllText(path)) ?? new List<GmProfile>();
            }
            catch (Exception)
            {
                return Array.Empty<GmProfile>();
            }
        }

        public GmProfile GetOrCreateDefault()
        {
            var profiles = LoadAll().ToList();
            var profile = profiles.FirstOrDefault();
            if (profile != null)
                return profile;

            profile = new GmProfile();
            Save(profile);
            return profile;
        }

        public void Save(GmProfile profile)
        {
            profile.Validate();
            var profiles = LoadAll().ToList();
            var existing = profiles.FindIndex(item => string.Equals(item.Id, profile.Id, StringComparison.Ordinal));
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            if (existing >= 0)
                profiles[existing] = profile;
            else
                profiles.Add(profile);

            var path = ProjectSettings.GlobalizePath(ProfilePath);
            File.WriteAllText(path, JsonSerializer.Serialize(profiles, _jsonOptions));
        }

        public GmProfile Find(string id)
            => LoadAll().FirstOrDefault(profile => string.Equals(profile.Id, id, StringComparison.Ordinal));
    }
}
