import os
import platform
import time
import webbrowser

VERSION = "Nyrion v1.3.1 - Lion 0.1.9 (Copyright GayWare64)"
command_history = []

# ────── Basic Display ──────
def show_logo():
    print("""
██        ██
████      ██    
██  ██    ██
██    ██  ██
██      ████
██        ██
██        ██
""")
    print(f"Welcome to {VERSION}\nType 'lion help' to begin.\n")

# ────── Command Handlers ──────
def show_help():
    print("""
Available Commands:
lion specs       - Show system specs
lion version     - Show version info
lion base        - Show OS info
lion web         - Open browser
lion edit        - Launch text editor
lion explore     - List files
lion newfile     - Create a new file
lion newfolder   - Create a new folder
lion read <file> - View a text file
lion chat        - Simple chatbot
lion chatlog     - View/save chatbot log
lion draw        - ASCII drawing tool
lion history     - Show recent commands
lion clear       - Clear screen
lion exit        - Quit shell
""")

def show_specs():
    print(f"OS: {platform.system()} {platform.release()}")
    print(f"CPU: {platform.processor()}")
    print(f"Python: {platform.python_version()}")

def show_version():
    print(f"Nyrion Shell - {VERSION}")

def show_base():
    print(f"Platform: {platform.platform()}")

def open_web():
    webbrowser.open("https://www.google.com")

def launch_editor():
    os.system("notepad" if os.name == "nt" else "nano")

def file_explorer():
    path = input("Path (leave blank for current): ").strip() or "."
    if not os.path.isdir(path):
        print("Invalid folder path.")
        return
    print(f"\nFiles in {os.path.abspath(path)}:")
    for item in os.listdir(path):
        print(" -", item)

def create_file():
    name = input("Enter new file name: ").strip()
    if not name:
        print("File name cannot be empty.")
        return
    with open(name, "w") as f:
        f.write("")
    print(f"File '{name}' created.")

def create_folder():
    name = input("Enter new folder name: ").strip()
    if not name:
        print("Folder name cannot be empty.")
        return
    os.makedirs(name, exist_ok=True)
    print(f"Folder '{name}' created.")

def read_file(filename):
    if not os.path.isfile(filename):
        print("File not found.")
        return
    with open(filename, "r") as f:
        print("\n--- File Content ---")
        print(f.read())
        print("---------------------")

chat_log = []
def chatbot():
    print("Chatbot: Type 'exit' to stop chatting.")
    while True:
        user = input("You: ").strip().lower()
        if user == "exit":
            print("Chatbot: Bye!")
            break
        elif "hello" in user:
            reply = "Hi there!"
        elif "time" in user:
            reply = "The time is " + time.strftime("%H:%M:%S")
        elif "name" in user:
            reply = "I'm NyrionBot!"
        else:
            reply = "I don't understand that yet."
        print("Chatbot:", reply)
        chat_log.append(f"You: {user}\nBot: {reply}\n")

def view_chatlog():
    if not chat_log:
        print("No chat history yet.")
        return
    with open("chatlog.txt", "w") as f:
        f.write("\n".join(chat_log))
    print("Chat log saved to 'chatlog.txt':")
    print("\n".join(chat_log[-5:]))

def ascii_draw():
    grid = [[" " for _ in range(20)] for _ in range(10)]
    print("ASCII Draw Mode. Type 'save' to save, 'exit' to quit.")
    while True:
        for row in grid:
            print("".join(row))
        cmd = input("Draw (x y char): ").strip()
        if cmd == "exit":
            break
        elif cmd == "save":
            with open("drawing.txt", "w") as f:
                for row in grid:
                    f.write("".join(row) + "\n")
            print("Drawing saved to 'drawing.txt'")
        else:
            try:
                x, y, ch = cmd.split()
                x, y = int(x), int(y)
                if 0 <= y < len(grid) and 0 <= x < len(grid[0]):
                    grid[y][x] = ch[0]
                else:
                    print("Out of bounds.")
            except:
                print("Invalid format. Use: x y char")

def show_history():
    print("Last 5 commands:")
    for cmd in command_history[-5:]:
        print(" -", cmd)

# ────── Main Loop ──────
def main():
    show_logo()
    while True:
        try:
            cmd = input(">> ").strip()
            if not cmd:
                continue
            command_history.append(cmd)

            if cmd == "lion help":
                show_help()
            elif cmd == "lion specs":
                show_specs()
            elif cmd == "lion version":
                show_version()
            elif cmd == "lion base":
                show_base()
            elif cmd == "lion web":
                open_web()
            elif cmd == "lion edit":
                launch_editor()
            elif cmd == "lion explore":
                file_explorer()
            elif cmd == "lion newfile":
                create_file()
            elif cmd == "lion newfolder":
                create_folder()
            elif cmd.startswith("lion read "):
                filename = cmd[10:].strip()
                read_file(filename)
            elif cmd == "lion chat":
                chatbot()
            elif cmd == "lion chatlog":
                view_chatlog()
            elif cmd == "lion draw":
                ascii_draw()
            elif cmd == "lion history":
                show_history()
            elif cmd == "lion clear":
                os.system("cls" if os.name == "nt" else "clear")
            elif cmd == "lion exit":
                print("Goodbye!")
                break
            else:
                print("Unknown command. Try 'lion help'.")
        except KeyboardInterrupt:
            print("\nType 'lion exit' to quit.")
        except Exception as e:
            print("Error:", e)

if __name__ == "__main__":
    main()
