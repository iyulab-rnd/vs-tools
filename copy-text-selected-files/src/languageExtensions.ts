import * as mime from "mime-types";
import * as path from "path";
import { BinaryDetector } from './binaryDetector';

// 언어별 확장자와 MIME 타입 매핑
interface LanguageConfig {
  extensions: string[];
  mimeTypes: string[];
}

interface LanguageExtensionsType {
  [key: string]: LanguageConfig;
}

// 알려진 텍스트 기반 파일 형식 정의
export const languageExtensions: LanguageExtensionsType = {
  typescript: {
    extensions: ['.ts', '.tsx'],
    mimeTypes: ['application/typescript', 'text/typescript']
  },
  javascript: {
    extensions: ['.js', '.jsx', '.mjs'],
    mimeTypes: ['application/javascript', 'text/javascript', 'application/x-javascript', 'application/ecmascript']
  },
  python: {
    extensions: ['.py', '.pyw', '.pyi'],
    mimeTypes: ['text/x-python', 'application/x-python']
  },
  java: {
    extensions: ['.java'],
    mimeTypes: ['text/x-java-source', 'application/x-java']
  },
  cpp: {
    extensions: ['.cpp', '.cc', '.cxx', '.hpp', '.h', '.c'],
    mimeTypes: ['text/x-c', 'text/x-c++']
  },
  csharp: {
    extensions: ['.cs'],
    mimeTypes: ['text/x-csharp']
  },
  go: {
    extensions: ['.go'],
    mimeTypes: ['text/x-go']
  },
  rust: {
    extensions: ['.rs'],
    mimeTypes: ['text/x-rust']
  },
  ruby: {
    extensions: ['.rb', '.rake', '.gemspec'],
    mimeTypes: ['text/x-ruby', 'application/x-ruby']
  },
  php: {
    extensions: ['.php', '.phtml', '.php3', '.php4', '.php5', '.phps'],
    mimeTypes: ['application/x-httpd-php', 'text/x-php']
  },
  swift: {
    extensions: ['.swift'],
    mimeTypes: ['text/x-swift']
  },
  kotlin: {
    extensions: ['.kt', '.kts'],
    mimeTypes: ['text/x-kotlin']
  },
  html: {
    extensions: ['.html', '.htm', '.xhtml'],
    mimeTypes: ['text/html', 'application/xhtml+xml']
  },
  css: {
    extensions: ['.css'],
    mimeTypes: ['text/css']
  },
  scss: {
    extensions: ['.scss'],
    mimeTypes: ['text/x-scss']
  },
  less: {
    extensions: ['.less'],
    mimeTypes: ['text/x-less']
  },
  xml: {
    extensions: ['.xml', '.xsd', '.xsl', '.xslt'],
    mimeTypes: ['text/xml', 'application/xml']
  },
  json: {
    extensions: ['.json'],
    mimeTypes: ['application/json']
  },
  yaml: {
    extensions: ['.yml', '.yaml'],
    mimeTypes: ['text/yaml', 'application/x-yaml', 'application/yaml']
  },
  markdown: {
    extensions: ['.md', '.markdown', '.mdown'],
    mimeTypes: ['text/markdown']
  },
  shell: {
    extensions: ['.sh', '.bash', '.zsh', '.fish'],
    mimeTypes: ['text/x-shellscript', 'application/x-sh']
  },
  sql: {
    extensions: ['.sql'],
    mimeTypes: ['text/x-sql', 'application/x-sql']
  },
  graphql: {
    extensions: ['.graphql', '.gql'],
    mimeTypes: ['application/graphql']
  },
  dockerfile: {
    extensions: ['dockerfile', '.dockerfile'],
    mimeTypes: ['text/x-dockerfile']
  },
  plaintext: {
    extensions: ['.txt', '.text'],
    mimeTypes: ['text/plain']
  }
};

// 명시적으로 제외할 파일 패턴
const EXCLUDED_PATTERNS = [
  /\.DS_Store/,
  /Thumbs\.db/
];

/**
 * 파일이 클립보드에 복사 가능한 텍스트 파일인지 확인하고 해당 언어/형식을 반환
 */
export function getFileFormat(filePath: string): string | null {
  // 1. 제외 패턴 체크
  if (isExcludedFile(filePath)) {
    return null;
  }

  const extension = path.extname(filePath).toLowerCase();

  // 2. 알려진 언어 확장자 체크
  const knownLanguage = getLanguageFromExtension(extension);
  if (knownLanguage) {
    return knownLanguage;
  }

  // 3. MIME 타입으로 텍스트 파일 여부 확인
  const mimeType = mime.lookup(filePath);
  if (mimeType) {
    // text/* 타입이거나 알려진 텍스트 기반 MIME 타입인 경우
    if (mimeType.startsWith('text/') || isTextMimeType(mimeType)) {
      // 확장자를 그대로 사용 (.은 제외)
      return extension.startsWith('.') ? extension.slice(1) : extension;
    }
  }

  // 4. 알 수 없는 형식의 경우 바이너리 검사
  if (!BinaryDetector.isFileBinary(filePath)) {
    // 바이너리가 아닌 경우 확장자를 형식으로 사용
    return extension.startsWith('.') ? extension.slice(1) : extension;
  }

  // 클립보드에 복사할 수 없는 파일
  return null;
}

/**
 * 알려진 언어 확장자에서 언어 찾기
 */
function getLanguageFromExtension(extension: string): string | null {
  for (const [language, config] of Object.entries(languageExtensions)) {
    if (config.extensions.includes(extension.toLowerCase())) {
      return language;
    }
  }
  return null;
}

/**
 * 알려진 텍스트 기반 MIME 타입인지 확인
 */
function isTextMimeType(mimeType: string): boolean {
  // application/json, application/xml 등 텍스트 기반 application 타입들
  const textBasedApplicationTypes = [
    'application/json',
    'application/xml',
    'application/javascript',
    'application/typescript',
    'application/x-yaml',
    'application/graphql'
  ];

  return textBasedApplicationTypes.includes(mimeType) ||
         Object.values(languageExtensions)
           .some(config => config.mimeTypes.includes(mimeType));
}

/**
 * 제외 패턴에 해당하는지 확인
 */
function isExcludedFile(filePath: string): boolean {
  return EXCLUDED_PATTERNS.some(pattern => pattern.test(filePath));
}