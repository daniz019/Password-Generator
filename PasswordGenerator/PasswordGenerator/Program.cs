using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

class Program
{
    static async Task Main(string[] args)
    {
        while (true)
        {
            await ShowMainMenu(); // Keep showing the main menu in a loop
        }
    }

    static async Task ShowMainMenu()
    {
        AnsiConsole.Clear();
        ShowAsciiArt();

        // Display the main menu options
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("O que você deseja fazer?")
                .PageSize(10)
                .AddChoices(new[] {
                "Gerar nova senha",
                "Visualizar senhas salvas",
                "Testar força de uma senha",
                "Sair"
                })
        );

        // Handle the user's choice
        switch (choice)
        {
            case "Gerar nova senha":
                await GeneratePassword();
                break;
            case "Visualizar senhas salvas":
                await ViewPasswords();
                break;
            case "Testar força de uma senha":
                await TestPasswordStrength();
                break;
            case "Sair":
                AnsiConsole.MarkupLine("[bold red]Saindo...[/]");
                Environment.Exit(0);
                break;
        }
    }

    static void ShowAsciiArt()
    {
        // Display ASCII art for the application
        AnsiConsole.Write(
            new FigletText("Password Manager")
                .Color(Color.Blue)
        );
        AnsiConsole.MarkupLine("[bold green]Bem-vindo ao Gerador de Senhas Seguras![/]");
    }

    static async Task GeneratePassword()
    {
        AnsiConsole.Clear();
        ShowAsciiArt();

        AnsiConsole.Write(new Rule("[yellow]Gerar Nova Senha[/]"));

        // Prompt the user for password length and validate it
        int length = AnsiConsole.Prompt(
            new TextPrompt<int>("Tamanho da senha:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]O tamanho mínimo da senha é 9.[/]")
                .Validate(length =>
                {
                    if (length < 9)
                    {
                        return ValidationResult.Error("[red]O tamanho mínimo da senha é 9.[/]");
                    }
                    return ValidationResult.Success();
                })
        );

        // Ask the user which character types to include
        bool includeUppercase = AnsiConsole.Confirm("Incluir letras maiúsculas?", true);
        bool includeLowercase = AnsiConsole.Confirm("Incluir letras minúsculas?", true);
        bool includeNumbers = AnsiConsole.Confirm("Incluir números?", true);
        bool includeSpecialChars = AnsiConsole.Confirm("Incluir caracteres especiais?", true);

        // Ensure at least one character type is selected
        if (!includeUppercase && !includeLowercase && !includeNumbers && !includeSpecialChars)
        {
            AnsiConsole.MarkupLine("[bold red]Erro: Pelo menos um tipo de caractere deve ser selecionado.[/]");
            await Task.Delay(2000);
            return;
        }

        // Simulate password generation with a loading animation
        await AnsiConsole.Status()
            .StartAsync("Gerando senha...", async ctx =>
            {
                await Task.Delay(1000);
            });

        // Generate the password
        string password = GenerateRandomPassword(length, includeUppercase, includeLowercase, includeNumbers, includeSpecialChars);

        AnsiConsole.MarkupLine($"[bold green]Senha gerada:[/] [yellow]{Markup.Escape(password)}[/]");

        // Evaluate and display the password strength
        double passwordStrength = EvaluatePasswordStrength(password);
        ShowPasswordStrength(passwordStrength);

        // Ask the user if they want to save the password
        bool savePassword = AnsiConsole.Confirm("Deseja salvar esta senha?");
        if (savePassword)
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task1 = ctx.AddTask("[green]Criptografando senha...[/]");
                    var task2 = ctx.AddTask("[green]Salvando senha...[/]");

                    while (!ctx.IsFinished)
                    {
                        await Task.Delay(100);
                        task1.Increment(10);
                        task2.Increment(10);
                    }
                });

            // Encrypt and save the password
            string encryptedPassword = Encrypt(password);
            SavePassword(encryptedPassword);
            AnsiConsole.MarkupLine("[bold green]Senha criptografada e salva com sucesso![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[bold red]Senha não foi salva.[/]");
        }

        // Ask the user if they want to return to the main menu
        bool returnToMenu = AnsiConsole.Confirm("Deseja voltar ao menu principal?");
        if (!returnToMenu)
        {
            AnsiConsole.MarkupLine("[bold red]Saindo...[/]");
            Environment.Exit(0);
        }
    }

    static async Task ViewPasswords()
    {
        AnsiConsole.Clear();
        ShowAsciiArt();

        AnsiConsole.Write(new Rule("[yellow]Senhas Salvas[/]"));

        // Define the path to the passwords file
        string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PasswordManager"
        );

        string filePath = Path.Combine(folderPath, "senhas.txt");

        // Check if the file exists and has content
        if (!File.Exists(filePath) || File.ReadAllLines(filePath).Length == 0)
        {
            var panel = new Panel("[red]Nenhuma senha salva ainda.[/]")
                .Border(BoxBorder.Rounded)
                .Header("[yellow]Aviso[/]")
                .HeaderAlignment(Justify.Center);
            AnsiConsole.Write(panel);
        }
        else
        {
            string[] encryptedPasswords = File.ReadAllLines(filePath);

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold blue]Senhas Descriptografadas[/]").Centered());

            // Simulate decryption with a loading animation
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Descriptografando senhas...[/]");

                    foreach (string encryptedPassword in encryptedPasswords)
                    {
                        string decryptedPassword = Decrypt(encryptedPassword);
                        table.AddRow($"[green]{Markup.Escape(decryptedPassword)}[/]");
                        await Task.Delay(500);
                        task.Increment(100 / encryptedPasswords.Length);
                    }
                });

            AnsiConsole.Write(table);
        }

        // Ask the user if they want to return to the main menu
        bool returnToMenu = AnsiConsole.Confirm("Deseja voltar ao menu principal?");
        if (!returnToMenu)
        {
            AnsiConsole.MarkupLine("[bold red]Saindo...[/]");
            Environment.Exit(0);
        }
    }

    static async Task TestPasswordStrength()
    {
        AnsiConsole.Clear();
        ShowAsciiArt();

        AnsiConsole.Write(new Rule("[yellow]Testar Força de uma Senha[/]"));

        // Prompt the user to enter a password
        string password = AnsiConsole.Prompt(
            new TextPrompt<string>("Digite a senha que deseja testar:")
                .Secret()
        );

        // Evaluate and display the password strength
        double passwordStrength = EvaluatePasswordStrength(password);
        ShowPasswordStrength(passwordStrength);

        // Provide recommendations to improve the password
        AnsiConsole.MarkupLine("[bold]Recomendações para melhorar a senha:[/]");

        bool hasRecommendation = false;

        if (password.Length < 12)
        {
            AnsiConsole.MarkupLine("[red]- Aumente o tamanho da senha para pelo menos 12 caracteres.[/]");
            hasRecommendation = true;
        }

        if (!password.Any(char.IsUpper))
        {
            AnsiConsole.MarkupLine("[red]- Adicione letras maiúsculas.[/]");
            hasRecommendation = true;
        }

        if (!password.Any(char.IsLower))
        {
            AnsiConsole.MarkupLine("[red]- Adicione letras minúsculas.[/]");
            hasRecommendation = true;
        }

        if (!password.Any(char.IsDigit))
        {
            AnsiConsole.MarkupLine("[red]- Adicione números.[/]");
            hasRecommendation = true;
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            AnsiConsole.MarkupLine("[red]- Adicione caracteres especiais (ex: !@#$%^&*).[/]");
            hasRecommendation = true;
        }

        if (IsCommonWeakPassword(password))
        {
            AnsiConsole.MarkupLine("[red]- Evite usar senhas comuns ou previsíveis.[/]");
            hasRecommendation = true;
        }

        if (ContainsSimpleSequence(password))
        {
            AnsiConsole.MarkupLine("[red]- Evite sequências simples (ex: 123456, abcdef).[/]");
            hasRecommendation = true;
        }

        if (ContainsDate(password))
        {
            AnsiConsole.MarkupLine("[red]- Evite usar datas (ex: 2504 para 25 de abril).[/]");
            hasRecommendation = true;
        }

        if (ContainsCommonName(password))
        {
            AnsiConsole.MarkupLine("[red]- Evite usar nomes comuns (ex: daniel, maria).[/]");
            hasRecommendation = true;
        }

        if (ContainsNameNumberPattern(password))
        {
            AnsiConsole.MarkupLine("[red]- Evite combinações de nomes e números (ex: daniel123).[/]");
            hasRecommendation = true;
        }

        if (ContainsCommonWord(password))
        {
            AnsiConsole.MarkupLine("[red]- Evite usar palavras comuns (ex: password, admin).[/]");
            hasRecommendation = true;
        }

        if (!hasRecommendation)
        {
            AnsiConsole.MarkupLine("[green]Sua senha está boa![/]");
        }

        // Ask the user if they want to return to the main menu
        bool returnToMenu = AnsiConsole.Confirm("Deseja voltar ao menu principal?");
        if (!returnToMenu)
        {
            AnsiConsole.MarkupLine("[bold red]Saindo...[/]");
            Environment.Exit(0);
        }
    }

    static string GenerateRandomPassword(int length, bool includeUppercase, bool includeLowercase, bool includeNumbers, bool includeSpecialChars)
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string numbers = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        // Build the allowed characters based on user preferences
        StringBuilder allowedChars = new StringBuilder();
        if (includeUppercase) allowedChars.Append(uppercase);
        if (includeLowercase) allowedChars.Append(lowercase);
        if (includeNumbers) allowedChars.Append(numbers);
        if (includeSpecialChars) allowedChars.Append(specialChars);

        if (allowedChars.Length == 0)
        {
            throw new ArgumentException("Pelo menos um tipo de caractere deve ser selecionado.");
        }

        // Generate the password randomly
        Random random = new Random();
        StringBuilder password = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(allowedChars.Length);
            password.Append(allowedChars[index]);
        }

        return password.ToString();
    }

    static void SavePassword(string encryptedPassword)
    {
        // Define the path to the passwords file
        string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PasswordManager"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "senhas.txt");

        // Save the encrypted password to the file
        File.AppendAllText(filePath, encryptedPassword + Environment.NewLine);
    }

    static string Encrypt(string text)
    {
        // Encrypt the text using AES encryption
        using (Aes aes = Aes.Create())
        {
            byte[] key = GenerateKey("ChaveSecreta12345", 32);
            aes.Key = key;
            aes.IV = new byte[16];

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(text);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }

    static string Decrypt(string encryptedText)
    {
        // Decrypt the text using AES decryption
        using (Aes aes = Aes.Create())
        {
            byte[] key = GenerateKey("ChaveSecreta12345", 32);
            aes.Key = key;
            aes.IV = new byte[16];

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }

    static byte[] GenerateKey(string password, int keySize)
    {
        // Generate a key using PBKDF2
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt: new byte[8], iterations: 10000, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(keySize);
        }
    }

    static double EvaluatePasswordStrength(string password)
    {
        // Evaluate the password strength based on various criteria
        double strength = 1.0;

        if (password.Length < 12)
        {
            strength -= 0.3;
        }

        if (!password.Any(char.IsUpper))
        {
            strength -= 0.1;
        }

        if (!password.Any(char.IsLower))
        {
            strength -= 0.1;
        }

        if (!password.Any(char.IsDigit))
        {
            strength -= 0.1;
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            strength -= 0.1;
        }

        if (IsCommonWeakPassword(password))
        {
            strength -= 0.5;
        }

        if (ContainsSimpleSequence(password))
        {
            strength -= 0.3;
        }

        if (ContainsDate(password))
        {
            strength -= 0.2;
        }

        if (ContainsCommonName(password))
        {
            strength -= 0.2;
        }

        if (ContainsNameNumberPattern(password))
        {
            strength -= 0.3;
        }

        if (ContainsCommonWord(password))
        {
            strength -= 0.4;
        }

        return Math.Max(0, Math.Min(strength, 1.0));
    }

    static void ShowPasswordStrength(double strength)
    {
        // Display the password strength visually
        string color;
        string description;

        if (strength < 0.5)
        {
            color = "red";
            description = "Fraca";
        }
        else if (strength < 0.8)
        {
            color = "yellow";
            description = "Média";
        }
        else
        {
            color = "green";
            description = "Forte";
        }

        int progress = (int)(strength * 100);
        int filledBars = (int)(strength * 10);
        filledBars = Math.Max(0, Math.Min(filledBars, 10));

        AnsiConsole.MarkupLine($"[bold]Força da Senha:[/] [{color}]{new string('█', filledBars)}{new string('░', 10 - filledBars)}[/] {progress}% ({description})");
    }

    static bool IsCommonWeakPassword(string password)
    {
        // Check if the password is in a list of common weak passwords
        string[] weakPasswords = {
            "123456", "password", "12345678", "qwerty", "123456789",
            "12345", "1234", "111111", "1234567", "dragon",
            "123123", "baseball", "abc123", "football", "monkey",
            "letmein", "696969", "shadow", "master", "666666"
        };

        return weakPasswords.Contains(password);
    }

    static bool ContainsSimpleSequence(string password)
    {
        // Check for simple sequences like "123456" or "qwerty"
        if (password.Contains("123456") || password.Contains("654321"))
        {
            return true;
        }

        if (password.Contains("qwerty") || password.Contains("ytrewq"))
        {
            return true;
        }

        return false;
    }

    static bool ContainsDate(string password)
    {
        // Check if the password contains a date in the format DDMM or MMDD
        for (int i = 0; i <= password.Length - 4; i++)
        {
            string substring = password.Substring(i, 4);
            if (int.TryParse(substring, out int number))
            {
                int day = number / 100;
                int month = number % 100;

                if ((day >= 1 && day <= 31) && (month >= 1 && month <= 12))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool ContainsCommonName(string password)
    {
        // Check if the password contains a common name
        string[] commonNames = {
            "daniel", "maria", "joao", "ana", "pedro",
            "carlos", "paulo", "lucas", "mariana", "julia",
            "andre", "rafael", "fernando", "gabriel", "lucia",
            "sandra", "patricia", "roberto", "ricardo", "felipe"
        };

        return commonNames.Any(name => password.ToLower().Contains(name));
    }

    static bool ContainsNameNumberPattern(string password)
    {
        // Check if the password contains a common name followed by numbers
        string[] commonNames = {
            "daniel", "maria", "joao", "ana", "pedro",
            "carlos", "paulo", "lucas", "mariana", "julia"
        };

        foreach (var name in commonNames)
        {
            if (password.ToLower().StartsWith(name) && password.Substring(name.Length).All(char.IsDigit))
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsCommonWord(string password)
    {
        // Check if the password contains a common word
        string[] commonWords = {
            "password", "admin", "welcome", "login", "letmein",
            "master", "sunshine", "shadow", "monkey", "football"
        };

        return commonWords.Any(word => password.ToLower().Contains(word));
    }
}