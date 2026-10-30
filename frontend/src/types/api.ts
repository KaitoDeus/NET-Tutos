export interface Category {
  id: number;
  name: string;
  slug: string;
  description: string;
  badgeColor: string;
  iconClass: string;
  tutorialCount: number;
}

export interface TutorialSummary {
  id: number;
  title: string;
  slug: string;
  summary: string;
  estimatedReadingMinutes: number;
  difficulty: string;
  categoryId?: number;
  categoryName: string;
  orderIndex: number;
  viewCount: number;
  isCompleted: boolean;
}

export interface QuizQuestion {
  id: number;
  questionText: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  correctOption: string;
  explanation: string;
}

export interface TutorialDetail {
  id: number;
  title: string;
  slug: string;
  summary: string;
  contentMarkdown: string;
  estimatedReadingMinutes: number;
  difficulty: string;
  categoryId?: number;
  categoryName: string;
  categorySlug: string;
  isCompleted: boolean;
  createdAt: string;
  previousTutorial?: TutorialSummary;
  nextTutorial?: TutorialSummary;
  quizzes: QuizQuestion[];
}

export interface CodeTemplate {
  id: string;
  title: string;
  description: string;
  code: string;
}

export interface CodingChallenge {
  id: number;
  title: string;
  slug: string;
  shortDescription: string;
  descriptionMarkdown: string;
  difficulty: string;
  orderIndex: number;
  initialCode: string;
  solutionCode?: string;
  xpReward: number;
  categoryName: string;
  totalTestCases: number;
  isPassed: boolean;
}

export interface CodeExecutionResponse {
  isSuccess: boolean;
  output: string;
  error?: string;
  executionTimeMs: number;
  memoryUsedBytes: number;
}

export interface ChallengeSubmissionResponse {
  allPassed: boolean;
  passedTestsCount: number;
  totalTestsCount: number;
  executionTimeMs: number;
  compileError?: string;
  xpEarned?: number;
}

export interface AiTutorChatResponse {
  success: boolean;
  responseMarkdown: string;
  codeSnippet?: string;
  suggestedFollowUps: string[];
  timestamp: string;
}

export interface UserProfile {
  id: string;
  username: string;
  email: string;
  fullName: string;
  avatarUrl: string;
  totalXp: number;
  level: number;
  streakDays: number;
  completedLessonsCount: number;
}
