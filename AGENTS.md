# Project Coding Standards

These rules apply to project-owned code throughout this repository, including Unity runtime and editor scripts. Preserve generated files and third-party code rather than rewriting them to match these conventions.

## Naming

- Use PascalCase for class names, such as `PlayerController` and `CameraController`. Long, descriptive, iOS-style names are acceptable.
- Use PascalCase for method names, such as `GetPlayerController`. Long, descriptive names are acceptable.
- Prefix member variables with `m_`, followed by PascalCase, such as `m_Character` and `m_PlayerController`.
- Use camelCase for local variables, such as `count` and `player`.
- For types used to represent JSON data, use lowercase snake_case member names instead of the `m_` convention, such as `level_name`, `width`, `height`, and `layout`.

## Readability and Language

- Prefer self-explanatory names over comments. Do not add comments for obvious or routine behavior; add them only where code would otherwise be ambiguous or confusing.
- Within a long method, a block serving a single purpose may be separated with `////////////////` lines above and below it to make that block clear.
- Do not add Chinese text to code, including comments and log messages. Use English instead.
- Do not introduce the literal citation artifact `[cite_start]` into code.
- End every text file with an empty line. Preserve this when creating or editing files.

## Namespace

- Use exactly `CR` for all project namespaces, including editor code.
- Do not introduce additional namespaces or subnamespaces such as `CR.Editor`; they can conflict with class names under `CR`.

## Examples

```csharp
namespace CR
{
    public class PlayerController
    {
        private Character m_Character;

        public void UpdateCharacter()
        {
            int count = 0;
        }
    }

    [System.Serializable]
    public class LevelJsonData
    {
        public string level_name;
        public int width;
        public int height;
        public string[][] layout;
    }
}
```

