import argparse
from pathlib import Path


def parse_args():
    # argparse.ArgumentParser：
    # 用来定义命令行参数。
    # description：当用户输入 -h 或 --help 时显示的说明文字。
    parser = argparse.ArgumentParser(
        description="批量清理 C# 源码注释，默认保留 XML 文档注释。"
    )

    # root：
    # 位置参数，也就是必须传入的参数。
    # 作用：指定要扫描的 C# 项目目录或解决方案目录。
    # type=Path：把用户输入的字符串路径转换成 pathlib.Path 对象。
    # Path 是 Python 里处理文件路径的对象，比直接拼字符串更安全。
    parser.add_argument(
        "root",
        type=Path,
        help="要处理的 C# 项目目录，例如 D:\\RiderProject\\YourProject"
    )

    # --dry-run：
    # 可选参数。
    # action="store_true"：只要命令里出现这个参数，值就是 True；没出现就是 False。
    # 作用：只预览哪些文件会被修改，不真正写回文件。
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="只预览会修改哪些文件，不真正写回"
    )

    # --remove-doc：
    # 可选参数。
    # 作用：默认保留 /// <summary> 这类 XML 文档注释；
    # 加上这个参数后，连 XML 文档注释也删除。
    # XML 文档注释：C# 里用 /// 或 /** */ 写的 API 说明，Rider 会识别它们。
    parser.add_argument(
        "--remove-doc",
        action="store_true",
        help="连 XML 文档注释也删除，例如 /// <summary>"
    )

    # --include-generated：
    # 可选参数。
    # 作用：默认跳过 .g.cs、.designer.cs、.generated.cs 这类自动生成文件；
    # 加上这个参数后，也会处理这些文件。
    # 自动生成文件：通常由工具或框架生成，不建议手动改。
    parser.add_argument(
        "--include-generated",
        action="store_true",
        help="包含自动生成文件，例如 .g.cs、.designer.cs、.generated.cs"
    )

    return parser.parse_args()


def should_skip_file(file_path: Path, root: Path, include_generated: bool) -> bool:
    # file_path：
    # 当前准备处理的 .cs 文件路径。
    #
    # root：
    # 用户传入的扫描根目录。
    #
    # include_generated：
    # True  = 处理自动生成文件。
    # False = 跳过自动生成文件。

    # relative_to：
    # 把完整路径转换成相对 root 的路径。
    # 例如 D:\A\B\File.cs 相对 D:\A 就是 B\File.cs。
    relative_path = file_path.relative_to(root)

    # parts：
    # 路径的每一层目录名。
    # 例如 B\File.cs 会变成 ("B", "File.cs")。
    parts = set(relative_path.parts)

    # 跳过这些目录：
    # bin：编译输出目录。
    # obj：编译中间目录。
    # .git：Git 内部目录。
    # .vs：Visual Studio / Rider 相关缓存目录。
    if {"bin", "obj", ".git", ".vs"} & parts:
        return True

    # suffixes：
    # 文件名的小写形式，用来判断是否是常见自动生成文件。
    lower_name = file_path.name.lower()

    if not include_generated:
        if (
            lower_name.endswith(".g.cs")
            or lower_name.endswith(".designer.cs")
            or lower_name.endswith(".generated.cs")
        ):
            return True

    return False


def read_text_safely(file_path: Path) -> str:
    # file_path：
    # 要读取的源码文件路径。
    #
    # utf-8-sig：
    # 可以兼容带 BOM 的 UTF-8 文件。
    # BOM 是文本文件开头的隐藏标记，有些 Windows 工具会生成它。
    try:
        return file_path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        # 如果不是 UTF-8，就退回系统默认编码。
        # errors="replace"：遇到无法识别的字符时用替代字符，避免脚本直接崩溃。
        return file_path.read_text(encoding="mbcs", errors="replace")


def write_text_safely(file_path: Path, text: str):
    # file_path：
    # 要写回的源码文件路径。
    #
    # text：
    # 清理注释后的源码文本。
    #
    # encoding="utf-8"：
    # 统一用 UTF-8 写回，方便跨平台。
    file_path.write_text(text, encoding="utf-8")


def remove_comments(source: str, keep_xml_doc_comments: bool) -> str:
    # source：
    # 一个 .cs 文件的完整源码文本。
    #
    # keep_xml_doc_comments：
    # True  = 保留 /// 和 /** */ 这类 XML 文档注释。
    # False = 删除所有注释。
    #
    # 返回值：
    # 删除注释后的源码文本。

    result = []

    # i：
    # 当前扫描到 source 的第几个字符。
    i = 0

    # n：
    # 源码总长度，避免反复调用 len(source)。
    n = len(source)

    while i < n:
        current = source[i]
        next_char = source[i + 1] if i + 1 < n else ""

        # 处理普通单行注释：// comment
        if current == "/" and next_char == "/":
            # XML 单行文档注释：/// <summary>
            is_xml_doc = i + 2 < n and source[i + 2] == "/"

            if keep_xml_doc_comments and is_xml_doc:
                # 保留整行 XML 文档注释，直到换行符。
                while i < n and source[i] not in "\r\n":
                    result.append(source[i])
                    i += 1
                continue

            # 删除普通单行注释。
            # 这里放一个空格，避免极端情况下两个代码片段被粘在一起。
            result.append(" ")

            # 跳过注释内容，但保留换行符。
            while i < n and source[i] not in "\r\n":
                i += 1
            continue

        # 处理普通多行注释：/* comment */
        if current == "/" and next_char == "*":
            # XML 多行文档注释：/** comment */
            is_xml_doc = i + 2 < n and source[i + 2] == "*"

            if keep_xml_doc_comments and is_xml_doc:
                # 保留整段 XML 文档注释。
                result.append(current)
                result.append(next_char)
                i += 2

                while i < n:
                    result.append(source[i])

                    # 找到 */ 说明注释结束。
                    if source[i] == "*" and i + 1 < n and source[i + 1] == "/":
                        result.append(source[i + 1])
                        i += 2
                        break

                    i += 1

                continue

            # 删除普通多行注释。
            # 保留换行符是为了尽量不改变源码行号。
            result.append(" ")
            i += 2

            while i < n:
                if source[i] == "*" and i + 1 < n and source[i + 1] == "/":
                    i += 2
                    break

                # 保留 Windows 换行：\r\n。
                if source[i] == "\r" and i + 1 < n and source[i + 1] == "\n":
                    result.append("\r")
                    result.append("\n")
                    i += 2
                    continue

                # 保留 Unix 换行：\n。
                if source[i] in "\r\n":
                    result.append(source[i])

                i += 1

            continue

        # 处理逐字字符串：@"C:\path\file"
        # 逐字字符串：C# 里以 @"..." 开头的字符串，里面的反斜杠不需要转义。
        if current == "@" and next_char == '"':
            result.append(current)
            result.append(next_char)
            i += 2

            while i < n:
                result.append(source[i])

                # 逐字字符串里 "" 表示一个真正的双引号，不代表字符串结束。
                if source[i] == '"' and i + 1 < n and source[i + 1] == '"':
                    result.append(source[i + 1])
                    i += 2
                    continue

                # 单独的 " 表示逐字字符串结束。
                if source[i] == '"':
                    i += 1
                    break

                i += 1

            continue

        # 处理普通字符串："text"
        # 普通字符串：C# 里最常见的字符串，里面的 \" 表示转义双引号。
        if current == '"':
            result.append(current)
            i += 1

            while i < n:
                result.append(source[i])

                # 反斜杠转义。
                # 例如 \" 里的 " 不是字符串结尾。
                if source[i] == "\\" and i + 1 < n:
                    result.append(source[i + 1])
                    i += 2
                    continue

                if source[i] == '"':
                    i += 1
                    break

                i += 1

            continue

        # 处理字符字面量：'/'
        # 字符字面量：C# 里用单引号包住的单个字符。
        if current == "'":
            result.append(current)
            i += 1

            while i < n:
                result.append(source[i])

                # 处理转义字符，例如 '\''。
                if source[i] == "\\" and i + 1 < n:
                    result.append(source[i + 1])
                    i += 2
                    continue

                if source[i] == "'":
                    i += 1
                    break

                i += 1

            continue

        # 其他普通代码字符直接保留。
        result.append(current)
        i += 1

    return "".join(result)


def main():
    args = parse_args()

    # resolve：
    # 把相对路径转换成绝对路径。
    root = args.root.resolve()

    if not root.exists():
        print(f"目录不存在：{root}")
        return

    if not root.is_dir():
        print(f"传入的不是目录：{root}")
        return

    # keep_xml_doc_comments：
    # 默认保留 XML 文档注释。
    # 如果用户传了 --remove-doc，就不保留。
    keep_xml_doc_comments = not args.remove_doc

    changed_count = 0

    # rglob("*.cs")：
    # 递归查找 root 下所有 .cs 文件。
    # 递归：包含子目录、子目录的子目录，以此类推。
    for file_path in root.rglob("*.cs"):
        if should_skip_file(file_path, root, args.include_generated):
            continue

        old_text = read_text_safely(file_path)
        new_text = remove_comments(old_text, keep_xml_doc_comments)

        if old_text == new_text:
            continue

        changed_count += 1

        if args.dry_run:
            print(f"会清理：{file_path}")
        else:
            write_text_safely(file_path, new_text)
            print(f"已清理：{file_path}")

    if args.dry_run:
        print(f"预览完成，共 {changed_count} 个文件会被修改。")
    else:
        print(f"清理完成，共修改 {changed_count} 个文件。")


if __name__ == "__main__":
    main()
