# Singapore Red Cross 360° Feedback Interactive Playbook

## Group Project Report
**Industry Partner:** Singapore Red Cross

**Institution:** Ngee Ann Polytechnic

**Project Title:** Interactive Culture and Climate Management Playbook for 360° Feedback Implementation

---
## Table of Contents

1. Introduction
2. Solution Synopsis
3. Solution Specifications
4. Solution Integration
5. Sustainability and Future Scalability
6. Limits and Recommendations
7. Conclusion
8. References

---
## 1. Introduction

### 1.1 Background

Organisations are increasingly adopting **360-degree feedback systems** as an alternative to traditional top-down performance reviews. Unlike conventional appraisal systems, 360-degree feedback collects performance evaluations from multiple sources, including managers, peers, and subordinates. While this provides a more comprehensive understanding of employee performance, its success depends heavily on organisational culture, psychological safety, and employees' willingness to participate honestly.

Singapore Red Cross identified the need for an engaging and sustainable learning solution to prepare employees and People Managers for this cultural transition. Existing training methods relied heavily on static documents and workshops, which were less effective in encouraging behavioural change or providing opportunities for users to practise feedback conversations.

To address these challenges, our team developed an **Interactive 360° Feedback Playbook**—a web application that combines immersive learning, gamification, scenario-based training, and augmented reality (AR) to help users develop the skills and confidence required for constructive workplace feedback.

---
### 1.2 Project Objectives

The primary objectives of the project are to:

- Develop an interactive web-based playbook to support the implementation of a 360-degree feedback system.
- Equip employees and managers with practical feedback skills through scenario-based learning.
- Foster a psychologically safe feedback culture using the proprietary CARE Framework.
- Improve learner engagement through gamification and interactive assessments.
- Provide managers with tools to monitor learning outcomes and maintain the playbook.

---
## 2. Solution Synopsis

### 2.1 Overview

The proposed solution is a responsive web application designed to support both employees and People Managers throughout their 360-degree feedback learning journey.

The playbook combines interactive learning modules, decision-based workplace scenarios, knowledge assessments, gamification, and AR-assisted learning into a single platform. Rather than presenting users with static reading materials, the playbook encourages experiential learning by allowing users to participate in simulated workplace situations and immediately observe the impact of their decisions.

The application consists of two interconnected systems:

- **Main Playbook** – Used by employees and managers for learning.
- **Manager Dashboard** – Used by authorised administrators to maintain learning content and review learner analytics.

Together, these systems provide both a learner-centric experience and an efficient administrative platform for long-term maintenance.

---
### 2.2 Key Components

The solution consists of several integrated components.

#### Homepage

The Homepage acts as the central navigation hub, providing access to all major features including Learning Modules, AR Cheatsheets, Leaderboard, and Profile.

Users are required to complete a **Pre-Learning Survey** before accessing any learning modules. After completing all modules, a **Post-Learning Survey** is administered to evaluate changes in users’ confidence and understanding.

**Benefits**
- Provides a structured learning pathway.
- Collects baseline and post-training data.
- Supports organisational evaluation of learning outcomes.

---
#### Learning Modules

The learning modules are organised according to the **CARE Framework** and represent the core educational component of the playbook.

Instead of passive reading, each module presents users with an interactive scenario where they make decisions during workplace conversations. Users experience the consequences of their choices through changes in their scenario performance statistics, reinforcing positive behaviours while encouraging reflection on less effective responses.

This experiential learning approach promotes greater engagement and retention than traditional text-based learning.

---
#### Interactive Scenarios

The playbook currently includes six workplace scenarios that simulate realistic feedback situations.

| Scenario	| Learning Objective |
| --------- | ------------------ |
| Documenting Employee Performance	| Develop objective documentation practices. |
| Handling Difficult Feedback Conversations	| Conduct psychologically safe feedback conversations. |
| Turning Feedback into Action	| Convert feedback into actionable improvement plans. |
| Receiving Constructive Feedback	| Encourage openness to receiving feedback. |
| Speaking Up and Growing Through Feedback	| Promote respectful upward and peer feedback. |
| Giving Positive Feedback	| Reinforce meaningful recognition through positive feedback. |

Each scenario incorporates branching dialogue and interactive decision-making opportunities, allowing users to practise workplace communication within a risk-free environment.

---
#### Interactive Activities

To maintain engagement and encourage active participation, each scenario incorporates interactive learning activities throughout the lesson. These activities reinforce key concepts before users proceed to the assessment quiz.

The interactive activities include:

- **Drag-and-Drop** exercises to organise information or match concepts.
- **Fact vs Opinion** activities to help users distinguish objective observations from subjective interpretations.
- **Multiple Choice Questions (MCQ)** to reinforce understanding of key principles.
Ranking activities that require users to arrange actions or responses in the most appropriate order.

These activities encourage users to actively apply what they have learned rather than simply consuming information passively.

---
#### Assessment Quizzes

Each learning module concludes with a single assessment quiz designed specifically for that scenario.

To provide variety throughout the learning journey, different scenarios utilise different assessment formats, including:

- Multiple Choice Questions (MCQ)
- Fact vs Opinion
- Drag-and-Drop
- Ranking

After completing the assessment, users receive:

- Quiz score
- Points earned
- Key learning takeaways

The awarded points contribute towards the Leaderboard, encouraging continuous participation throughout the playbook.

---
#### AR Quick Reference Cheatsheets

One of the unique features of the playbook is the integration of **Augmented Reality (AR)** learning resources.

Users scan QR codes using their mobile devices to launch an AR experience featuring the Singapore Red Cross mascot. The mascot presents concise visual cheatsheets covering:

- The CARE Framework
- Giving constructive feedback
- Receiving feedback effectively

These resources provide users with convenient, just-in-time learning support that can be accessed outside formal training sessions, reinforcing key concepts whenever needed.

---
#### Leaderboard

The Leaderboard introduces gamification into the learning experience by ranking users according to the cumulative points earned from assessment quizzes.

Rather than rewarding speed, the leaderboard encourages consistent participation and motivates employees to complete all available learning modules.

This friendly competitive element helps sustain user engagement while promoting continuous learning across the organisation.

---
#### Profile

The Profile page allows users to manage their personal account information.

Users may:

- View their username, email address, and profile picture.
- Update their username.
- Reset their password securely.

Passwords are encrypted and stored using **Firebase Authentication**, ensuring user credentials remain protected.

---
#### Manager Dashboard

The Manager Dashboard is a separate administrative portal designed for authorised People Managers and HR personnel.

Managers can:

- Monitor learner participation.
- Review survey responses.
- Create and edit learning scenarios.
- Manage assessment quizzes.
- Update AR cheatsheets.

This architecture allows future content updates without modifying the application’s source code, improving long-term maintainability.

---

### 2.3 CARE Framework

The **CARE Framework** forms the pedagogical foundation of the playbook.

| Stage |	Purpose |
| ----- | ------- |
| Clarify	| Gather objective evidence and prepare for feedback conversations. |
| Address	| Deliver respectful, psychologically safe feedback. |
| Respond	| Develop actionable improvement plans through reflection. |
| Enhance	| Reinforce continuous growth through follow-up and recognition. |

Each learning module is aligned with one or more stages of the framework, ensuring that users gradually develop the competencies required for effective workplace feedback.

---

## 3. Solutions Specifications

### 3.1 Benefits

The Interactive Playbook provides several advantages over traditional training methods:

- Encourages experiential learning through decision-based scenarios.
- Improves learner engagement using gamification and interactive activities.
- Reinforces learning through varied assessment formats.
- Provides just-in-time support through AR cheatsheets.
- Enables managers to monitor organisational learning through analytics.
- Supports both employees and People Managers with role-specific content.

---
### 3.2 Applications

The solution can be applied in several organisational contexts, including:

- Onboarding programmes for new employees.
- Managerial communication and leadership training.
- Organisational culture transformation initiatives.
- Performance development preparation.
- Continuous professional development programmes.

---
### 3.3 Unique Features

The playbook includes several features that distinguish it from conventional e-learning systems:

#### Role-Based Learning

Content is adapted based on whether the user is an **Employee** or a **People Manager**, ensuring that learning materials remain relevant to the user’s responsibilities.

#### Behavioural Decision System

User decisions directly affect scenario performance statistics, encouraging reflective learning and reinforcing appropriate workplace behaviours.

#### AR-Enabled Reinforcement

The integration of QR-code-triggered AR cheatsheets provides mobile-friendly, just-in-time learning support that extends learning beyond the web application itself.

---
## 4. Solution Integration

The playbook is designed to complement Singapore Red Cross’s existing **Human Resource and performance development workflows** rather than replace them.

Employees complete the learning modules before participating in actual 360-degree feedback exercises, allowing HR teams to establish a consistent understanding of effective feedback practices across the organisation.

Managers can simultaneously monitor learner progress and survey results through the administrative dashboard, supporting data-informed decisions while minimising disruption to existing HR processes.

---
## 5. Sustainability and Future Scalability

The solution has been designed with **long-term sustainability** in mind.

The separation between the learner-facing application and the Manager Dashboard enables administrators to update scenarios, quizzes, and AR cheatsheets independently of the application’s source code.

### 5.1 Sustainability Features

- Centralised content management through the Manager Dashboard.
- Cloud-based hosting using Firebase Hosting.
- Secure authentication using Firebase Authentication.
- Scalable cloud database using Firebase Firestore.

---
### 5.2 Future Scalability Opportunities

Potential future enhancements include:

- Additional workplace scenarios.
- AI-assisted feedback coaching.
- Adaptive learning pathways based on user performance.
- Integration with Learning Management Systems (LMS).
- Microsoft Teams integration.
- Multilingual support.
- Advanced learner analytics and reporting.

## 6. Limits and Recommendations

### 6.1 Current Limitations

Although the playbook meets the primary project objectives, several limitations remain.

#### Separate Administrative Portal

The Manager Dashboard currently operates as a separate application from the learner-facing playbook, requiring administrators to access a different portal for content management.

#### AR Accessibility Constraints

The AR functionality relies on QR codes and compatible mobile devices, which may limit accessibility for users without camera-enabled devices or sufficient device permissions.

#### Limited Analytics

The current implementation focuses primarily on completion tracking and survey responses. Advanced analytics such as individual competency trends, behavioural insights, and personalised learning recommendations are not yet available.

#### Gamification Considerations

While the leaderboard can motivate many users, it may discourage individuals who consistently rank lower if not carefully managed.

---
### 6.2 Recommendations

To address the identified limitations, the following improvements are recommended:

#### Technical Recommendations
- Integrate the Manager Dashboard directly into the main application.
- Implement real-time analytics and reporting features.
- Add offline support for selected learning resources.

#### Learning Recommendations
- Introduce adaptive learning pathways based on learner performance.
- Provide personalised feedback and learning recommendations.
- Expand the library of workplace scenarios to cover additional organisational situations.

#### Accessibility Recommendations
- Provide non-AR alternatives for cheatsheet access.
- Introduce multilingual support for diverse user groups.
- Improve mobile accessibility for older devices.

## 7. Conclusion

The **Interactive 360° Feedback Playbook** successfully addresses Singapore Red Cross’s objective of preparing employees and People Managers for the implementation of a 360-degree feedback system through engaging, technology-enhanced learning experiences.

By combining interactive scenarios, decision-based learning, varied assessment activities, gamification, AR-assisted learning, and the CARE Framework, the playbook encourages users to develop constructive feedback behaviours while fostering a psychologically safe organisational culture.

The project delivers a maintainable and scalable web-based learning platform that supports both individual skill development and organisational culture transformation. The inclusion of the Manager Dashboard ensures that Singapore Red Cross can continue updating and expanding the playbook beyond the project handover phase.

Although opportunities remain for further enhancement—particularly in analytics, integration, and accessibility—the current solution provides a strong foundation for supporting Singapore Red Cross’s long-term feedback culture and continuous learning initiatives.

---
## 8. References

The following references informed the design and development of the Interactive 360° Feedback Playbook:

- Amy Edmondson. The Fearless Organization: Creating Psychological Safety in the Workplace for Learning, Innovation, and Growth.
- Center for Creative Leadership (CCL). 360-Degree Feedback and Leadership Development.
- Chartered Institute of Personnel and Development (CIPD). Performance Management Factsheet.
- Dale Carnegie. How to Win Friends and Influence People.
- Gallup. Workplace Engagement Research.
- Harvard Business Review. The Right Way to Give Feedback.
- SCARF Model – David Rock.
- SHRM (Society for Human Resource Management). 360-Degree Feedback Best Practices.
- SMART Goals Framework.
- SBI (Situation–Behaviour–Impact) Feedback Model.
