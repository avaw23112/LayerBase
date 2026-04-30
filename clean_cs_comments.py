import argparse
import re
from pathlib import Path


def parse_args():
    # ArgumentParser：
    # Python 标准库里的命令行参数解析器。
    # 它负责把用户输入的命令行参数转换成脚本里可用的对象。
    parser = argparse.ArgumentParser(
        description="批量清理 C# 注释，支持普通注释和 XML 文档注释。"
    )

    # root：
    # 必填参数。
    # 作用：指定要扫描的 C# 项目目录或解决方案目录。
    # type=Path：把字符串路径转换成 Path 对象。
    # Path：Python 里专门处理文件路径的对象。
    parser.add_argument(
        "root",
        type=Path,
        help="要处理的 C# 项目目录，例如 D:\\RiderProject\\YourProject"
    )

    # --dry-run：
    # 可选参数。
    # action='store_true' 表示：只要命令里出现 --dry-run，这个值就是 True。
    # 作用：只预览会修改哪些文件，不真正写回源码。
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="只预览会修改哪些文件，不真正写回"
    )

    # --normal：
    # 可选参数。
    # choices 限制用户只能输入 keep 或 remove。
    # keep：保留普通注释。
    # remove：删除普通注释。
    # 普通注释指 // 注释 和 /* */ 注释，不包括 /// XML 文档注释。
    parser.add_argument(
        "--normal",
        choices=["keep", "remove"],
        default="remove",
        help="普通注释处理方式：keep=保留，remove=删除，默认 remove"
    )

    # --doc：
    # 可选参数。
    # 作用：控制 XML 文档注释。
    # keep：保留所有 XML 文档注释。
    # remove：删除所有 XML 文档注释。
    # empty：只删除空的 XML 文档注释。
    #
    # 空的 XML 文档注释示例：
    # /// <summary>
    # ///
    # /// </summary>
    #
    # 非空 XML 文档注释示例：
    # /// <summary>
    # /// 获取用户信息
    # /// </summary>
    parser.add_argument(
        "--doc",
        choices=["keep", "remove", "empty"],
        default="keep",
        help="XML 文档注释处理方式：keep=保留，remove=删除全部，empty=只删空文档注释，默认 keep"
    )

    # --include-generated：
    # 可选参数。
    # 作用：默认跳过自动生成文件；加上它后也会处理自动生成文件。
    #
    # 自动生成文件：
    # 通常由工具、框架、设计器生成的 .cs 文件。
    # 常见文件名包括 .g.cs、.designer.cs、.generated.cs。
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
    # True  = 不跳过自动生成文件。
    # False = 跳过自动生成文件。

    relative_path = file_path.relative_to(root)

    # parts：
    # 路径的每一层目录名和文件名。
    # 例如 A/B/File.cs 会变成 ("A", "B", "File.cs")。
    parts = set(relative_path.parts)

    # bin：编译输出目录。
    # obj：编译中间目录。
    # .git：Git 内部目录。
    # .vs：IDE 缓存目录。
    if {"bin", "obj", ".git", ".vs"} & parts:
        return True

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
    # 要读取的 C# 源码文件路径。
    #
    # utf-8-sig：
    # 兼容带 BOM 的 UTF-8 文件。
    # BOM 是文本文件开头的隐藏标记，有些 Windows 工具会生成它。
    try:
        return file_path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        # mbcs：
        # Windows 下的系统默认编码。
        # errors='replace'：遇到无法识别的字符时用替代字符，避免脚本崩溃。
        return file_path.read_text(encoding="mbcs", errors="replace")


def write_text_safely(file_path: Path, text: str):
    # file_path：
    # 要写回的源码文件路径。
    #
    # text：
    # 清理后的源码文本。
    #
    # encoding='utf-8'：
    # 使用 UTF-8 写回文件。
    file_path.write_text(text, encoding="utf-8")


def preserve_newlines(text: str) -> str:
    # text：
    # 被删除的注释文本。
    #
    # 作用：
    # 删除注释内容时，尽量保留里面的换行符。
    # 这样可以减少源码行号变化，方便后续看 git diff。
    result = []

    i = 0
    while i < len(text):
        if text[i] == "\r" and i + 1 < len(text) and text[i + 1] == "\n":
            result.append("\r\n")
            i += 2
            continue

        if text[i] in "\r\n":
            result.append(text[i])

        i += 1

    # 前面加一个空格：
    # 避免注释被删除后，左右两边的代码 token 粘在一起。
    # token 是源码里的最小语法单位，比如变量名、关键字、括号、分号。
    return " " + "".join(result)


def collect_single_line_comment(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # 注释开始位置，也就是第一个 / 的位置。
    #
    # 返回值：
    # 第一个值是收集到的整行注释文本。
    # 第二个值是注释结束后的新扫描位置。
    i = start_index
    n = len(source)
    result = []

    while i < n and source[i] not in "\r\n":
        result.append(source[i])
        i += 1

    return "".join(result), i


def collect_xml_line_doc_block(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # XML 单行文档注释开始位置，也就是 /// 的第一个 /。
    #
    # 作用：
    # 收集连续的 /// 文档注释块。
    # 例如：
    # /// <summary>
    # /// 说明
    # /// </summary>
    #
    # 返回值：
    # 第一个值是完整 XML 文档注释块。
    # 第二个值是块结束后的新扫描位置。
    i = start_index
    n = len(source)
    result = []

    while i < n:
        line_start = i

        # 收集当前这一行。
        while i < n and source[i] not in "\r\n":
            result.append(source[i])
            i += 1

        # 收集换行符。
        if i < n and source[i] == "\r" and i + 1 < n and source[i + 1] == "\n":
            result.append("\r\n")
            i += 2
        elif i < n and source[i] in "\r\n":
            result.append(source[i])
            i += 1

        # 判断下一行是否还是 XML 文档注释。
        # 允许下一行前面有空格或 tab。
        lookahead = i
        while lookahead < n and source[lookahead] in " \t":
            lookahead += 1

        if not source.startswith("///", lookahead):
            break

        # 保留下一行开头的缩进。
        while i < lookahead:
            result.append(source[i])
            i += 1

    return "".join(result), i


def collect_multi_line_comment(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # 多行注释开始位置，也就是 /* 的第一个 /。
    #
    # 返回值：
    # 第一个值是完整多行注释文本。
    # 第二个值是注释结束后的新扫描位置。
    i = start_index
    n = len(source)
    result = []

    while i < n:
        result.append(source[i])

        if source[i] == "*" and i + 1 < n and source[i + 1] == "/":
            result.append(source[i + 1])
            i += 2
            break

        i += 1

    return "".join(result), i


def is_empty_xml_doc(doc_text: str) -> bool:
    # doc_text：
    # 一整段 XML 文档注释。
    #
    # 作用：
    # 判断这段文档注释是否“没有实际说明内容”。
    #
    # 注意：
    # 这里会保留 <inheritdoc />、<include /> 这类虽然没有正文但有语义的标签。
    # 语义：这里指它们会影响 IDE 提示或文档生成，不是纯空内容。

    lower_text = doc_text.lower()

    # 这些标签通常有实际文档意义，不能简单当成空注释删掉。
    meaningful_tags = [
        "<inheritdoc",
        "<include",
        "<see ",
        "<seealso",
        "<param ",
        "<typeparam ",
        "<returns",
        "<exception ",
        "<value",
        "<remarks",
        "<example",
    ]

    if any(tag in lower_text for tag in meaningful_tags):
        return False

    lines = doc_text.splitlines()
    cleaned_parts = []

    for line in lines:
        # 去掉行首空白。
        stripped = line.strip()

        # 处理 /// 开头的 XML 单行文档注释。
        if stripped.startswith("///"):
            stripped = stripped[3:].strip()

        # 处理 /** */ 中常见的每行 * 前缀。
        if stripped.startswith("/**"):
            stripped = stripped[3:].strip()

        if stripped.startswith("/*"):
            stripped = stripped[2:].strip()

        if stripped.endswith("*/"):
            stripped = stripped[:-2].strip()

        if stripped.startswith("*"):
            stripped = stripped[1:].strip()

        cleaned_parts.append(stripped)

    text = "\n".join(cleaned_parts)

    # 删除 XML 标签。
    # XML 标签：例如 <summary>、</summary>、<returns>。
    text = re.sub(r"<[^>]+>", "", text)

    # 删除常见 XML 转义。
    # XML 转义：例如 &lt; 表示 <，&gt; 表示 >。
    text = (
        text.replace("&lt;", "")
        .replace("&gt;", "")
        .replace("&amp;", "")
        .replace("&quot;", "")
        .replace("&apos;", "")
    )

    # 如果去掉标签 and 空白后没有文字，就认为是空文档注释。
    return text.strip() == ""


def should_keep_doc_comment(doc_text: str, doc_mode: str) -> bool:
    # doc_text：
    # 当前扫描到的 XML 文档注释文本。
    #
    # doc_mode：
    # keep   = 保留所有 XML 文档注释。
    # remove = 删除所有 XML 文档注释。
    # empty  = 只删除空 XML 文档注释。
    if doc_mode == "keep":
        return True

    if doc_mode == "remove":
        return False

    if doc_mode == "empty":
        return not is_empty_xml_doc(doc_text)

    return True


def consume_regular_string(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # 普通字符串开始位置，也就是 " 的位置。
    #
    # 普通字符串：
    # 例如 "hello \"world\""。
    # 里面的 \" 表示转义双引号，不代表字符串结束。
    i = start_index
    n = len(source)
    result = [source[i]]
    i += 1

    while i < n:
        result.append(source[i])

        if source[i] == "\\" and i + 1 < n:
            result.append(source[i + 1])
            i += 2
            continue

        if source[i] == '"':
            i += 1
            break

        i += 1

    return "".join(result), i


def consume_verbatim_string(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # 逐字字符串开始位置，通常是 @ 的位置。
    #
    # 逐字字符串：
    # C# 里以 @"..." 开头的字符串。
    # 它里面的反斜杠不需要转义，例如 @"C:\Temp\File.txt"。
    i = start_index
    n = len(source)
    result = []

    result.append(source[i])
    result.append(source[i + 1])
    i += 2

    while i < n:
        result.append(source[i])

        if source[i] == '"' and i + 1 < n and source[i + 1] == '"':
            result.append(source[i + 1])
            i += 2
            continue

        if source[i] == '"':
            i += 1
            break

        i += 1

    return "".join(result), i


def consume_char_literal(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # 字符字面量开始位置，也就是 ' 的位置。
    #
    # 字符字面量：
    # C# 里用单引号表示的单个字符，例如 '/'、'\n'。
    i = start_index
    n = len(source)
    result = [source[i]]
    i += 1

    while i < n:
        result.append(source[i])

        if source[i] == "\\" and i + 1 < n:
            result.append(source[i + 1])
            i += 2
            continue

        if source[i] == "'":
            i += 1
            break

        i += 1

    return "".join(result), i


def consume_raw_string(source: str, start_index: int) -> tuple[str, int]:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # start_index：
    # raw string literal 开始位置。
    #
    # raw string literal：
    # C# 11 引入的原始字符串。
    # 常见形式是：
    # var json = """
    # {
    #   "url": "https://example.com"
    # }
    # """;
    #
    # 它里面可以直接出现 // 或 /* */，不能当成注释删除。
    i = start_index
    n = len(source)
    result = []

    # 先收集可选的 $。
    # $ 用于字符串插值，例如 $"""hello {name}"""。
    while i < n and source[i] == "$":
        result.append(source[i])
        i += 1

    # 统计连续双引号数量。
    quote_count = 0
    while i < n and source[i] == '"':
        result.append(source[i])
        quote_count += 1
        i += 1

    # 不足三个双引号就不是 raw string。
    if quote_count < 3:
        return "".join(result), i

    closing_quotes = '"' * quote_count

    while i < n:
        if source.startswith(closing_quotes, i):
            result.append(closing_quotes)
            i += quote_count
            break

        result.append(source[i])
        i += 1

    return "".join(result), i


def is_raw_string_start(source: str, index: int) -> bool:
    # source：
    # 当前 .cs 文件的完整源码文本。
    #
    # index：
    # 当前扫描位置。
    #
    # 作用：
    # 判断当前位置是否是 C# raw string literal 的开始。
    i = index
    n = len(source)

    while i < n and source[i] == "$":
        i += 1

    return i + 2 < n and source[i:i + 3] == '"""'


def remove_comments(source: str, normal_mode: str, doc_mode: str) -> str:
    # source：
    # 一个 .cs 文件的完整源码文本。
    #
    # normal_mode：
    # keep   = 保留普通注释。
    # remove = 删除普通注释。
    #
    # doc_mode：
    # keep   = 保留 XML 文档注释。
    # remove = 删除所有 XML 文档注释。
    # empty  = 只删除空 XML 文档注释。
    #
    # 返回值：
    # 清理后的源码文本。

    result = []
    i = 0
    n = len(source)

    while i < n:
        current = source[i]
        next_char = source[i + 1] if i + 1 < n else ""

        # 处理 raw string literal，避免误删字符串里的 // 或 /* */。
        if current in '$"' and is_raw_string_start(source, i):
            text, i = consume_raw_string(source, i)
            result.append(text)
            continue

        # 处理逐字字符串 @"..."。
        if current == "@" and next_char == '"':
            text, i = consume_verbatim_string(source, i)
            result.append(text)
            continue

        # 处理 $@"..." 或 @$"..." 这种插值逐字字符串。
        if (
            current == "$"
            and i + 2 < n
            and source[i + 1] == "@"
            and source[i + 2] == '"'
        ):
            text, new_i = consume_verbatim_string(source, i + 1)
            result.append("$")
            result.append(text)
            i = new_i
            continue

        if (
            current == "@"
            and i + 2 < n
            and source[i + 1] == "$"
            and source[i + 2] == '"'
        ):
            text, new_i = consume_verbatim_string(source, i)
            result.append(text)
            i = new_i
            continue

        # 处理普通字符串 "..."。
        if current == '"':
            text, i = consume_regular_string(source, i)
            result.append(text)
            continue

        # 处理普通插值字符串 $"..."。
        if current == "$" and next_char == '"':
            result.append("$")
            text, i = consume_regular_string(source, i + 1)
            result.append(text)
            continue

        # 处理字符字面量，例如 '/'。
        if current == "'":
            text, i = consume_char_literal(source, i)
            result.append(text)
            continue

        # 处理单行注释。
        if current == "/" and next_char == "/":
            is_xml_doc = i + 2 < n and source[i + 2] == "/"

            if is_xml_doc:
                doc_text, i = collect_xml_line_doc_block(source, i)

                if should_keep_doc_comment(doc_text, doc_mode):
                    result.append(doc_text)
                else:
                    result.append(preserve_newlines(doc_text))

                continue

            comment_text, i = collect_single_line_comment(source, i)

            if normal_mode == "keep":
                result.append(comment_text)
            else:
                result.append(" ")

            continue

        # 处理多行注释。
        if current == "/" and next_char == "*":
            is_xml_doc = i + 2 < n and source[i + 2] == "*"
            comment_text, i = collect_multi_line_comment(source, i)

            if is_xml_doc:
                if should_keep_doc_comment(comment_text, doc_mode):
                    result.append(comment_text)
                else:
                    result.append(preserve_newlines(comment_text))
            else:
                if normal_mode == "keep":
                    result.append(comment_text)
                else:
                    result.append(preserve_newlines(comment_text))

            continue

        result.append(current)
        i += 1

    return "".join(result)


def main():
    args = parse_args()

    # resolve：
    # 把相对路径转换成绝对路径。
    # 例如 . 会变成当前目录的完整路径。
    root = args.root.resolve()

    if not root.exists():
        print(f"目录不存在：{root}")
        return

    if not root.is_dir():
        print(f"传入的不是目录：{root}")
        return

    changed_count = 0

    # rglob("*.cs")：
    # 递归查找所有 .cs 文件。
    # 递归：包含子目录、子目录的子目录，以此类推。
    for file_path in root.rglob("*.cs"):
        if should_skip_file(file_path, root, args.include_generated):
            continue

        old_text = read_text_safely(file_path)

        new_text = remove_comments(
            source=old_text,
            normal_mode=args.normal,
            doc_mode=args.doc
        )

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
