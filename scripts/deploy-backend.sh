#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${DEPLOY_PATH:-}" ]]; then
  echo "DEPLOY_PATH is required."
  exit 1
fi

if [[ -z "${AWS_REGION:-}" ]]; then
  echo "AWS_REGION is required."
  exit 1
fi

if [[ -z "${ECR_REGISTRY:-}" ]]; then
  echo "ECR_REGISTRY is required."
  exit 1
fi

if [[ -z "${ECR_API_REPOSITORY:-}" ]]; then
  echo "ECR_API_REPOSITORY is required."
  exit 1
fi

if [[ -z "${ECR_WORKER_REPOSITORY:-}" ]]; then
  echo "ECR_WORKER_REPOSITORY is required."
  exit 1
fi

if [[ -z "${IMAGE_TAG:-}" ]]; then
  echo "IMAGE_TAG is required."
  exit 1
fi

cd "${DEPLOY_PATH}"

if [[ ! -f "docker-compose.prod.yml" ]]; then
  echo "docker-compose.prod.yml is missing in ${DEPLOY_PATH}"
  exit 1
fi

aws ecr get-login-password --region "${AWS_REGION}" \
  | docker login --username AWS --password-stdin "${ECR_REGISTRY}"

export ECR_REGISTRY
export ECR_API_REPOSITORY
export ECR_WORKER_REPOSITORY
export IMAGE_TAG

docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d

docker container prune -f
docker image prune -a -f
docker builder prune -a -f
