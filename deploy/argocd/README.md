# ArgoCD Manifests

Each environment (staging, prod) will have its own `Application` manifest pointing at the appropriate
Helm chart version + values overlay. Concrete manifests are written when the first service ships
in Phase P1 (IdentityService); only the skeleton is committed here in P0.

Reference layout (planned):

```
deploy/argocd/
├── staging/
│   ├── app-identity.yaml
│   ├── app-requirement.yaml
│   └── ...
└── prod/
    ├── app-identity.yaml
    └── ...
```
